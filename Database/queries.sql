delimiter $$

drop function if exists get_close_price;

create function get_close_price(_symbol varchar(10), _date date)
returns decimal(20, 5) deterministic
return
(
	select
		nullif(close_price, 0)
	from price
	where
	(
		(_symbol is null and symbol is null)
		or symbol = _symbol
	)
	and date >= _date
	order by date
	limit 1
);

drop function if exists get_performance;

create function get_performance(_symbol varchar(10), _from date, _to date)
returns decimal(20, 5) deterministic
return
	ifnull(get_close_price(_symbol, _to) / get_close_price(_symbol, _from), 0)
	- get_close_price(null, _to) / get_close_price(null, _from);
	
drop procedure if exists get_facts_by_ratio;

create procedure get_facts_by_ratio(_divisor varchar(300), _from date, _to date, _form varchar(10), _horizon int)
begin
	select
		cik,
		filed,
		name,
		ratio,
		get_performance(symbol, filed, date_add(filed, interval _horizon month)) as performance
	from
	(
		select
			F1.cik,
			ticker.symbol,
			F1.filed,
			F1.name,
			F1.value / F2.value as ratio,
			row_number() over
			(
				partition by F1.cik, filed, name
				order by F1.end_date desc, F1.start_date desc
			) as group_rank
		from fact as F1 join ticker
		on F1.cik = ticker.cik
		join fact as F2
		on F1.cik = F2.cik and F1.filed = F2.filed and F1.form = F2.form
		where
			F1.form = _form
			and F1.filed >= _from
			and F1.filed < _to
			and F1.unit = 'USD'
			and F1.name <> _divisor
			and F2.name = _divisor
			-- and F2.value >= 1000000
			and ticker.exclude = 0
	) as F
	where group_rank = 1
	order by cik, filed, name;
end;

drop procedure if exists get_fact_frequency;

create procedure get_fact_frequency(_from date, _to date, _form varchar(10))
begin
	select
		name,
		unit,
		count(*) as count
	from
		fact join ticker
		on fact.cik = ticker.cik
	where
		exclude = 0
		and form = _form
		and filed >= _from
		and filed < _to
	group by
		name,
		unit
	order by count desc;
end;