-- Determine distribution of form types
with cik_form as
(
	select distinct cik, form, filed
	from fact
)
select
	form,
	count(*),
	round(count(*)::numeric / (select count(*) from cik_form) * 100, 2) as percentage
from cik_form
group by form
order by count desc