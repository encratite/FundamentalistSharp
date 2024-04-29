if object_id('price') is not null
	drop table price

if object_id('fact') is not null
	drop table fact

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
	value decimal(28) not null,
	fiscal_year int,
	fiscal_period char(2),
	form varchar(7) not null,
	filed date not null,
	frame varchar(9)
)

create clustered index fact_form_filed_cik_index on fact (form, filed, cik)
-- drop index fact_filed_form_index on fact

create table price
(
	symbol varchar(10) references ticker (symbol) on delete cascade,
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