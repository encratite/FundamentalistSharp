load data infile 'C:\\ML\\CSV\\sec_number.csv' into table sec_number
columns terminated by ','
optionally enclosed by '"'
lines terminated by '\r\n'
ignore 1 lines;

load data infile 'C:\\ML\\CSV\\sec_submission.csv' into table sec_submission
columns terminated by ','
optionally enclosed by '"'
lines terminated by '\r\n'
ignore 1 lines;

load data infile 'C:\\ML\\Sharadar\\SHARADAR_TICKERS_2.csv' into table ticker
columns terminated by ','
ignore 1 lines
(
	@ignore,
	@ignore,
	ticker,
	name,
	@ignore,
	@isdelisted,
	category,
	@ignore,
	@ignore,
	@sicsector,
	@sicindustry,
	@ignore,
	@famaindustry,
	@sector,
	@industry,
	@scalemarketcap,
	@scalerevenue,
	@relatedtickers,
	currency,
	@location,
	@ignore,
	@ignore,
	@ignore,
	@ignore,
	@ignore,
	@ignore,
	@secfilings,
	@ignore
)
set
	is_delisted = if(@isdelisted = 'Y', 1, 0),
	sic_sector = nullif(@sicsector, ''),
	sic_industry = nullif(@sicindustry, ''),
	fama_industry = nullif(@famaindustry, ''),
	sector = nullif(@sector, ''),
	industry = nullif(@industry, ''),
	market_cap = nullif(substring(@scalemarketcap, 1, 1), ''),
	revenue = nullif(substring(@scalerevenue, 1, 1), ''),
	country = replace(
		if(
			locate(';', @location) > 0,
			trim(substring(@location, locate(';', @location) + 1)),
			nullif(@location, '')
		),
		'U.S.A',
		'US'
	),
	state = if(
		locate(';', @location) > 0,
		substring(@location, 1, locate(';', @location) - 1),
		null
	),
	cik = regexp_substr(@secfilings, '[1-9][0-9]+'),
	related_tickers = nullif(@relatedtickers, '');

load data infile 'C:\\ML\\PriceData\\^GSPC.csv' into table price
columns terminated by ','
ignore 1 lines
(
	date,
	open_price,
	high,
	low,
	close_price,
	adjusted_close,
	volume
)
set
	ticker = null,
	unadjusted_close = null;

load data infile 'C:\\ML\\Sharadar\\SHARADAR_SEP_SORTED.csv' into table price
columns terminated by ','
(
	ticker,
	date,
	open_price,
	high,
	low,
	close_price,
	volume,
	adjusted_close,
	unadjusted_close,
	@ignore
);