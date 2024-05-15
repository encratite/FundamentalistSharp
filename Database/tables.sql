drop table if exists price;
drop table if exists fact;
drop table if exists market_cap;
drop table if exists ticker;

create table ticker
(
	symbol varchar(10) primary key,
	cik int not null,
	company varchar(80) not null,
	sector varchar(100),
	industry varchar(100),
	exclude tinyint not null
) engine = MyISAM;

create index ticker_symbol_exclude_index on ticker (symbol, exclude);

create table fact
(
	cik int not null,
	name varchar(300) not null,
	unit varchar(20) not null,
	start_date date,
	end_date date not null,
	value decimal(30, 6) not null,
	fiscal_year int,
	fiscal_period char(2),
	form varchar(7) not null,
	filed date not null,
	frame varchar(9)
) engine = MyISAM;

create index fact_form_filed_cik_index on fact (form, filed, cik);
create index fact_filed_index on fact (filed);
create index fact_name_filed_index on fact (name(200), filed);
create index fact_cik_form_filed_index on fact (cik, form, filed);

create table price
(
	symbol varchar(10) references ticker (symbol),
	date date not null,
	open_price decimal(20, 5) not null,
	high decimal(20, 5) not null,
	low decimal(20, 5) not null,
	close_price decimal(20, 5) not null,
	adjusted_close decimal(20, 5) not null,
	volume bigint not null
) engine = MyISAM;

create index price_date_index on price (date);
create index price_symbol_date_index on price (symbol, date);

create table market_cap
(
	symbol varchar(10) references ticker (symbol),
	date date not null,
	market_cap bigint not null
) engine = MyISAM;

create index market_cap_symbol_date_index on market_cap (symbol, date);