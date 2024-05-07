if object_id('get_form_frequency') is not null
	drop procedure get_form_frequency
if object_id('get_facts_performance_distribution') is not null
	drop procedure get_facts_performance_distribution
if object_id('get_price_performance_distribution') is not null
	drop procedure get_price_performance_distribution
if object_id('get_price_performance_distribution_by_period') is not null
	drop procedure get_price_performance_distribution_by_period
if object_id('get_failed_stock_stats') is not null
	drop procedure get_failed_stock_stats
if object_id('get_failed_stocks') is not null
	drop procedure get_failed_stocks
if object_id('get_form_performance') is not null
	drop procedure get_form_performance
if object_id('get_market_cap_performance') is not null
	drop procedure get_market_cap_performance
go

if object_id('get_ohlc') is not null
	drop function get_ohlc
if object_id('get_close_price') is not null
	drop function get_close_price
if object_id('get_price_performance_rows') is not null
	drop function get_price_performance_rows
if object_id('get_price_performance_rows') is not null
	drop function get_price_performance_rows
if object_id('get_symbol') is not null
	drop function get_symbol
if object_id('get_performance') is not null
	drop function get_performance
if object_id('get_industry_sector_stats') is not null
	drop function get_industry_sector_stats
if object_id('get_industry_stats') is not null
	drop function get_industry_stats
if object_id('get_label') is not null
	drop function get_label
go

if type_id('performance_table_type') is not null
	drop type performance_table_type
if type_id('get_label_prices_type') is not null
	drop type get_label_prices_type
go

create type performance_table_type as table
(
	price money,
	performance decimal,
	volume money
)
go

create type get_label_prices_type as table
(
	date date index date_index,
	stock_open money,
	stock_close money,
	index_open money,
	index_close money
)
-- with (memory_optimized = on)
go

create function get_ohlc(@symbol varchar(10), @date date)
returns table as
return
(
	select top 1
		nullif(open_price, 0) as open_price,
		nullif(high, 0) as high,
		nullif(low, 0) as low,
		nullif(close_price, 0) as close_price,
		nullif(volume, 0) as volume
	from price
	where
	(
		(@symbol is null and symbol is null)
		or symbol = @symbol
	)
	and date >= @date
	order by date
)
go

create function get_close_price(@symbol varchar(10), @date date)
returns money as
begin
	return
	(
		select top 1
			nullif(close_price, 0)
		from price
		where
		(
			(@symbol is null and symbol is null)
			or symbol = @symbol
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
		avg(volume) as volume,
		count(*) as count
	from
	(
		select
			iif(@group_size is not null, floor(price / @group_size) * @group_size, @from) as group_key,
			performance,
			volume
		from @performance
		where
			performance is not null
			and (@from is null or price >= @from)
			and (@to is null or price < @to)
	) P
	group by group_key
go

create function get_symbol(@cik int)
returns varchar(10) as
begin
	return
	(
		select top 1 symbol
		from ticker
		where
			cik = @cik
			and exclude = 0
	)
end
go

create function get_performance(@symbol varchar(10), @from date, @to date)
returns money as
begin
	return
		dbo.get_close_price(@symbol, @to) / dbo.get_close_price(@symbol, @from)
		- dbo.get_close_price(null, @to) / dbo.get_close_price(null, @from)
end
go

create function get_industry_sector_stats(@from date, @to date)
returns table as
return
	select
		industry,
		sector,
		avg(performance) performance,
		count(*) count
	from
	(
		select
			industry,
			sector,
			least(dbo.get_performance(ticker.symbol, @from, @to), 10) as performance
		from ticker
		where
			exclude = 0
			and industry is not null
			and sector is not null
	) T
	where performance is not null
	group by industry, sector
go

create function get_industry_stats(@from date, @to date)
returns table as
return
	select
		industry,
		avg(performance) performance,
		count(*) count
	from
	(
		select
			industry,
			least(dbo.get_performance(ticker.symbol, @from, @to), 10) as performance
		from ticker
		where
			exclude = 0
			and industry is not null
			and sector is not null
	) T
	where performance is not null
	group by industry
go

create function get_label(@symbol varchar(10), @from date, @upper money, @lower money, @days int)
returns money as
begin
	declare @prices get_label_prices_type
	insert into @prices
	select top (@days)
		P1.date,
		P1.open_price as stock_open,
		P1.close_price as stock_close,
		P2.open_price as index_open,
		P2.close_price as index_close
	from
		price as P1 join price as P2
		on P1.date = P2.date
	where
		P1.date >= @from
		and P1.symbol = @symbol
		and P2.symbol is null
		and P2.date >= @from
	order by P1.date

	declare @first_date date
	declare @first_stock_open money
	declare @first_index_open money
	select top 1
		@first_date = date,
		@first_stock_open = stock_open,
		@first_index_open = index_open
	from @prices
	order by date

	declare @performance money
	select top 1
		@performance = greatest(least(performance, @upper), @lower)
	from
	(
		select
			date,
			stock_close / @first_stock_open - index_close / @first_index_open as performance
		from @prices
		where date > @first_date
	) as P
	where
		performance < @lower
		or performance > @upper
		or date = (select max(date) from @prices)
	order by date

	return @performance
end
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

create procedure get_facts_performance_distribution(@from date, @to date, @forecast_days int, @form varchar(7)) as
begin
	declare @group_size int = 10;

	with fact_count as
	(
		select top 100 percent
			-- dbo.get_performance(dbo.get_symbol(cik), filed,  dateadd(d, @forecast_days, filed)) as performance,
			dbo.get_label(dbo.get_symbol(cik), dateadd(day, 1, filed), 0.1, -0.1, @forecast_days) as performance,
			(count(*) / @group_size) * @group_size as group_key
		from fact
		where
			(@from is null or fact.filed >= @from)
			and (@to is null or fact.filed < @to)
			and form = @form
		group by cik, form, filed
	)
	select top 100 percent
		group_key as facts,
		format(avg(performance), 'N3') as performance,
		count(*) as count
	from fact_count
	where performance is not null
	group by group_key
	order by group_key
end
go

create procedure get_price_performance_distribution(@from date, @to date, @format bit) as
begin
	declare @performance performance_table_type

	-- This is flawed in that it doesn't properly deal with prices and volumes of low liquidity stock
	insert into @performance
	select
		ohlc_from.close_price price,
		(
			dbo.get_close_price(symbol, @to) / ohlc_from.close_price
			- dbo.get_close_price(null, @to) / dbo.get_close_price(null, @from)
		) as performance,
		ohlc_from.close_price * ohlc_from.volume as volume
	from ticker
	cross apply get_ohlc(symbol, @from) as ohlc_from
	where exclude = 0

	declare @output table
	(
		price money,
		performance money,
		volume money,
		count bigint
	)

	insert into @output
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

	if @format = 1
	begin
		select
			price,
			format(performance, 'N2') as performance,
			format(volume, 'N0') as volume,
			count
		from @output
	end
	else
	begin
		select * from @output
	end
end
go

create procedure get_price_performance_distribution_by_period(@from date, @to date, @months int) as
begin
	declare @output table
	(
		price money,
		performance money,
		volume money,
		count bigint
	)

	declare @now date = @from
	while @now < @to
	begin
		declare @next date = dateadd(month, @months, @now)
		insert into @output
		exec get_price_performance_distribution @now, @next, 0
		set @now = @next
	end

	select
		price,
		format(avg(performance), 'N2') as performance,
		format(avg(volume), 'N0') as volume,
		cast(avg(count) as bigint) count
	from @output
	group by price
	order by price
end
go

create procedure get_failed_stock_stats as
begin
	declare @all int =
	(
		select count(*)
		from
		(
			select distinct price.symbol
			from
				price join ticker
				on price.symbol = ticker.symbol
			where ticker.exclude = 0
		) C
	)

	declare @failed int =
	(
		select count(*)
		from
		(
			select
				price.symbol,
				max(date) as date
			from
				price join ticker
				on price.symbol = ticker.symbol
			where ticker.exclude = 0
			group by price.symbol
			having max(date) < dateadd(month, -1, (select top 1 max(date) from price))
		) C
	)

	select
		@all as all_stocks,
		@failed as failed_stocks,
		format(@failed / cast(@all as decimal), 'N3') as failure_rate
end
go

create procedure get_failed_stocks as
begin
	select
		P1.symbol,
		max(date) as date,
		coalesce(
			dbo.get_close_price(P1.symbol, dateadd(year, -1, (select top 1 max(date) from price))),
			(select top 1 close_price from price as P2 where P1.symbol = P2.symbol order by date)
		) as price
	from
		price as P1 join ticker
		on P1.symbol = ticker.symbol
	where ticker.exclude = 0
	group by P1.symbol
	having max(date) < dateadd(month, -1, (select top 1 max(date) from price))
	order by date desc
end
go

create procedure get_form_performance(@from date, @to date, @forecast int) as
begin
	select
		form,
		avg(performance) as performance,
		count(performance) as count
	from
	(
		select
			F2.form,
			-- dbo.get_performance(ticker.symbol, F2.filed, dateadd(day, @forecast, F2.filed)) as performance
			dbo.get_label(ticker.symbol, dateadd(day, 1, F2.filed), 0.1, -0.1, @forecast) as performance
		from
			(
				select distinct form, cik, filed
				from fact
				where filed >= @from and filed < @to
			) F2 join ticker
			on F2.cik = ticker.cik
		where ticker.exclude = 0
	) F1
	group by form
	order by performance desc
end
go

create procedure get_market_cap_performance(@from date, @to date) as
begin
	with M as
	(
		select
			(
				select top 1
					(case
						when market_cap < 100 then 0
						when market_cap < 500 then 100
						when market_cap < 1000 then 500
						when market_cap < 2500 then 1000
						when market_cap < 5000 then 2500
						when market_cap < 10000 then 5000
						when market_cap < 50000 then 10000
						when market_cap < 100000 then 50000
						when market_cap < 500000 then 100000
						when market_cap < 1000000 then 500000
						when market_cap < 5000000 then 1000000
						else 5000000
					end) as market_cap
				from market_cap
				where
					ticker.symbol = market_cap.symbol
					and market_cap.date <= @from 
				order by market_cap.date desc
			) as market_cap,
			-- least(dbo.get_performance(ticker.symbol, @from, @to), 10) as performance
			dbo.get_label(ticker.symbol, dateadd(day, 1, @from), 0.5, -0.1, datediff(day, @from, @to)) as performance
		from ticker
		where
			ticker.exclude = 0
			and exists (select * from market_cap where ticker.symbol = market_cap.symbol and market_cap.date <= @from)
	)
	select
		market_cap,
		avg(performance) as performance,
		count(*) as count
	from M
	group by M.market_cap
	order by market_cap
end
go