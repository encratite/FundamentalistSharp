drop table company cascade;
drop table fact;
drop table price;

create table company
(
	cik int primary key,
	symbol text not null,
	name text not null
);

create table fact
(
	id serial primary key,
	cik int references company (cik) not null,
	name text not null,
	end_date date not null,
	value numeric not null,
	fiscal_year int,
	fiscal_period char(2),
	form text not null,
	filed date not null,
	frame text
);

create index fact_cik_filed_form_index on fact (cik, filed, form);

create table price
(
	id serial primary key,
	cik int references company (cik) not null,
	date date not null,
	open money not null,
	high money not null,
	low money not null,
	close money not null,
	volume bigint not null
);