drop table if exists sec_submission;

create table sec_submission
(
	adsh char(20) not null,
	cik int not null,
	form varchar(7) not null,
	period date,
	fiscal_year int,
	fiscal_period char(2),
	filed date not null,
	accepted datetime not null
) engine = MyISAM;

create index sec_submission_form_filed_index on sec_submission (form, filed);

drop table if exists sec_number;

create table sec_number
(
	adsh char(20) not null,
	tag varchar(256) not null,
	end_date date not null,
	quarters tinyint not null,
	unit varchar(20) not null,
	value decimal(30, 6) not null
) engine = MyISAM;

create index sec_number_adsh_index on sec_number (adsh);

drop table if exists ticker;

create table ticker
(
	cik int,
	ticker varchar(10) not null,
	name varchar(200) not null,
	is_delisted tinyint not null,
	category varchar(50) not null,
	sic_sector varchar(100),
	sic_industry varchar(100),
	fama_industry varchar(100),
	sector varchar(100),
	industry varchar(100),
	market_cap tinyint,
	revenue tinyint,
	currency varchar(5) not null,
	country varchar(50),
	state varchar(50),
	related_tickers varchar(300)
);

create index ticker_cik_index on ticker (cik);
create index ticker_ticker_index on ticker (ticker);

drop table if exists price;

create table price
(
	ticker varchar(10),
	date date not null,
	open_price decimal(20, 5) not null,
	high decimal(20, 5) not null,
	low decimal(20, 5) not null,
	close_price decimal(20, 5) not null,
	volume decimal(20, 5) not null,
	adjusted_close decimal(20, 5) not null,
	unadjusted_close decimal(20, 5)
) engine = MyISAM;

create index price_ticker_date_index on price (ticker, date);

drop view if exists stock;

create view stock as
select distinct
	cik,
	ticker
from ticker
where
	cik is not null
	and category in
	(
		'Domestic Common Stock',
		'Domestic Common Stock Primary Class',
		'Domestic Common Stock Secondary Class'
	);