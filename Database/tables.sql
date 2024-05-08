if object_id('price') is not null
	drop table price

if object_id('fact') is not null
	drop table fact

if object_id('market_cap') is not null
	drop table market_cap

if object_id('ticker') is not null
	drop table ticker

create table ticker
(
	symbol varchar(10) primary key,
	cik int not null,
	company varchar(80) not null,
	sector varchar(100),
	industry varchar(100),
	exclude bit not null
)

create index ticker_symbol_exclude_index on ticker (symbol, exclude)

create table fact
(
	cik int not null,
	name varchar(300) not null,
	end_date date not null,
	value decimal(38, 9) not null,
	fiscal_year int,
	fiscal_period char(2),
	form varchar(7) not null,
	filed date not null,
	frame varchar(9)
)

create clustered index fact_form_filed_cik_index on fact (form, filed, cik)
create index fact_filed_index on fact (filed)
create index fact_name_filed_index on fact (name, filed)
create index fact_cik_form_filed_index on fact (cik, form, filed)

create table price
(
	symbol varchar(10) references ticker (symbol),
	date date not null,
	open_price money not null,
	high money not null,
	low money not null,
	close_price money not null,
	adjusted_close money not null,
	volume bigint not null
)

create index price_date_index on price (date)
create index price_symbol_date_index on price (symbol, date)

create table market_cap
(
	symbol varchar(10) references ticker (symbol) not null,
	date date not null,
	-- Multiples of $100k dollars
	market_cap int not null
)

create clustered index market_cap_symbol_date_index on market_cap (symbol, date)