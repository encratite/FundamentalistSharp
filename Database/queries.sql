delimiter $$

drop function if exists get_close_price;

create function get_close_price(_ticker varchar(10), _date date)
returns decimal(20, 5) deterministic
return
(
	select
		nullif(close_price, 0)
	from price
	where
	(
		(_ticker is null and ticker is null)
		or ticker = _ticker
	)
	and date >= _date
	order by date
	limit 1
);

drop function if exists get_performance;

create function get_performance(_ticker varchar(10), _from date, _to date)
returns decimal(20, 5) deterministic
begin
	declare _ticker_performance decimal(20, 5) default ifnull(get_close_price(_ticker, _to) / get_close_price(_ticker, _from), 0);
	declare _index_performance decimal(20, 5) default get_close_price(null, _to) / get_close_price(null, _from);
	return _ticker_performance - _index_performance;
end;

drop procedure if exists get_tag_performance;

create procedure get_tag_performance(_from date, _to date, _form varchar(10), _horizon int)
begin
	select
		TV.cik,
		filed,
		tag,
		unit,
		value,
		get_performance(S.ticker, filed, date_add(filed, interval _horizon month)) as performance
	from
	(
		select
			S.cik,
			S.filed,
			N.tag,
			N.unit,
			N.value,
			row_number() over
			(
				partition by S.cik, S.filed, N.tag
				order by N.end_date desc
			) as group_rank
		from
			sec_submission as S
			join sec_number as N
			on S.adsh = N.adsh
		where
			S.form = _form
			and S.filed >= _from
			and S.filed < _to
			and N.unit in ('USD', 'shares', 'pure')
			and (_form <> '10-K' or N.quarters in (0, 4))
			and (_form <> '10-Q' or N.quarters in (0, 1))
	) as TV
	join stock as S on TV.cik = S.cik
	where
		group_rank = 1
		and S.market_cap >= 3
	order by cik, filed, tag;
end;
	
drop procedure if exists get_tag_ratio_performance;

create procedure get_tag_ratio_performance(_divisor varchar(256), _from date, _to date, _form varchar(10), _horizon int)
begin
	select
		TV.cik,
		filed,
		tag,
		ratio as value,
		get_performance(S.ticker, filed, date_add(filed, interval _horizon month)) as performance
	from
	(
		select
			S.cik,
			S.filed,
			N1.tag,
			N1.value / N2.value as ratio,
			row_number() over
			(
				partition by S.cik, S.filed, N1.tag
				order by N1.end_date desc
			) as group_rank
		from
			sec_submission as S
			join sec_number as N1
			on S.adsh = N1.adsh
			join sec_number as N2
			on S.adsh = N2.adsh
		where
			S.form = _form
			and S.filed >= _from
			and S.filed < _to
			and N1.tag <> _divisor
			and N1.unit = 'USD'
			and N2.tag = _divisor
			and N2.unit = 'USD'
			and N1.end_date = N2.end_date
			and (_form <> '10-K' or (N1.quarters in (0, 4) and N2.quarters in (0, 4)))
			and (_form <> '10-Q' or (N1.quarters in (0, 1) and N2.quarters in (0, 1)))
	) as TV
	join stock as S on TV.cik = S.cik
	where 
		group_rank = 1
		and S.market_cap >= 3
	order by cik, filed, tag;
end;

drop procedure if exists get_fact_frequency;

create procedure get_fact_frequency(_from date, _to date, _form varchar(10))
begin
	select
		N.tag,
		N.unit,
		count(*) as count
	from
		sec_submission as S join sec_number as N
		on S.adsh = N.adsh
	where
		S.form = _form
		and S.filed >= _from
		and S.filed < _to
	group by
		N.tag,
		N.unit
	order by count desc;
end;