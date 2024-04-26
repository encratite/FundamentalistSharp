if object_id('get_form_frequency') is not null
	drop procedure get_form_frequency
if object_id('get_facts_performance_distribution') is not null
	drop procedure get_facts_performance_distribution
if object_id('get_price') is not null
	drop function get_price
if object_id('get_price_performance_rows') is not null
	drop function get_price_performance_rows
if type_id('performance_table_type') is not null
	drop type performance_table_type
go

create type performance_table_type as table
(
	price money,
	performance decimal
)
go

create function get_price(@cik int, @date date)
returns money as
begin
	return
	(
		select top 1
			nullif(close_price, 0)
		from price
		where
			(
				(@cik is null and cik is null)
				or cik = @cik
			)
			and date >= @date
		order by date
	)
end
go

create function get_price_performance_rows(@from int, @to int, @group_size money, @performance performance_table_type readonly)
returns table as
return
	select
		group_key as price,
		avg(performance) as performance,
		count(*) as count
	from
	(
		select
			iif(@group_size is not null, floor(price / @group_size) * @group_size, @from) as group_key,
			performance
		from @performance
		where
			performance is not null
			and (@from is null or price >= @from)
			and (@to is null or price < @to)
	) P
	group by group_key
go

create procedure get_form_frequency as
begin
	with cik_form as
	(
		select distinct cik, form, filed
		from fact
	)
	select top 100 percent
		form,
		count(*) count,
		format(cast(count(*) as decimal) / (select count(*) from cik_form) * 100, 'N2') as percentage
	from cik_form
	group by form
	order by count desc
end
go

create procedure get_facts_performance_distribution(@from date, @to date, @forecast_days int) as
begin
	declare @group_size int = 10;

	with fact_count as
	(
		select top 100 percent
			(
				dbo.get_price(cik, dateadd(d, @forecast_days, filed)) / dbo.get_price(cik, filed)
				- dbo.get_price(null, dateadd(d, @forecast_days, filed)) / dbo.get_price(null, filed)
			) as performance,
			(count(*) / @group_size) * @group_size as group_key
		from fact
		where
			(@from is null or filed >= @from) and
			(@to is null or filed < @to)
		group by cik, form, filed
	)
	select top 100 percent
		group_key as facts,
		format(avg(performance), 'N2') as performance,
		count(*) as count
	from fact_count
	where performance is not null
	group by group_key
	order by group_key
end
go

create procedure get_price_performance_distribution(@from date, @to date) as
begin
	declare @performance performance_table_type

	insert into @performance
	select
		dbo.get_price(cik, @from) price,
		(
			dbo.get_price(cik, @to) / dbo.get_price(cik, @from)
			- dbo.get_price(null, @to) / dbo.get_price(null, @from)
		) as performance
	from company

	select * from
	(
		select * from get_price_performance_rows(0, 1, 0.2, @performance)
		union all
		select * from get_price_performance_rows(1, 5, 1, @performance)
		union all
		select * from get_price_performance_rows(5, 20, 5, @performance)
		union all
		select * from get_price_performance_rows(20, 50, 10, @performance)
		union all
		select * from get_price_performance_rows(50, 200, 50, @performance)
		union all
		select * from get_price_performance_rows(200, 500, 100, @performance)
		union all
		select * from get_price_performance_rows(500, 1000, 500, @performance)
		union all
		select * from get_price_performance_rows(1000, null, null, @performance)
	) P
	order by price
end
go