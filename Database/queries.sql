-- Determine distribution of form types
with cik_form as
(
	select distinct cik, form, filed
	from fact
)
select
	form,
	count(*) count,
	format(cast(count(*) as decimal) / (select count(*) from cik_form) * 100, 'N2') percentage
from cik_form
group by form
order by count desc