if object_id('get_form_frequency') is not null
	drop function get_form_frequency
if object_id('get_price') is not null
	drop function get_price
if object_id('get_facts_performance_distribution') is not null
	drop function get_facts_performance_distribution
go

create function get_form_frequency()
returns table as
return
(
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
)
go

create function get_price(@cik int, @date date)
returns money as
begin
	return
	(
		select top 1
			nullif(open_price, 0)
		from price
		where
			cik = @cik and
			date >= @date
		order by date
	)
end
go

create function get_facts_performance_distribution(@from date, @to date, @forecast_days int)
returns @output table
(
	facts int,
	performance text,
	count bigint
) as
begin
	declare @group_size int = 10;

	with fact_count as
	(
		select top 100 percent
			dbo.get_price(cik, dateadd(d, @forecast_days, filed)) / dbo.get_price(cik, filed) - 1 as performance,
			(count(*) / @group_size) * @group_size as group_key
		from fact
		where
			(@from is null or filed >= @from) and
			(@to is null or filed < @to)
		group by cik, form, filed
	)
	insert into @output
	select top 100 percent
		group_key as facts,
		format(avg(performance), 'N2') performance,
		count(*) as count
	from fact_count
	where performance is not null
	group by group_key
	order by group_key
	option (force order)

	return
end
go