if object_id('price') is not null
	drop table price

if object_id('fact') is not null
	drop table fact

if object_id('company') is not null
	drop table company

create table company
(
	cik int primary key,
	symbol text not null,
	name text not null
)

create table fact
(
	cik int references company (cik) on delete cascade not null,
	name text not null,
	end_date date not null,
	value decimal(28) not null,
	fiscal_year int,
	fiscal_period char(2),
	form varchar(7) not null,
	filed date not null,
	frame text
)

create clustered index fact_cik_filed_form_index on fact (cik, filed, form)

create table price
(
	cik int references company (cik) on delete cascade not null,
	date date not null,
	open_price money not null,
	high money not null,
	low money not null,
	close_price money not null,
	volume bigint not null
)