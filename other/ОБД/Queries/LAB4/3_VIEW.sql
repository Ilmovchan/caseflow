CREATE OR REPLACE VIEW first_time_clients AS
SELECT 
    c.id AS client_id,
    c.first_name,
    c.last_name,
    MIN(ca.start_date) AS first_case_date
FROM 
    "client" c
JOIN 
    "case" ca ON c.id = ca.client_id
GROUP BY 
    c.id, c.first_name, c.last_name
HAVING 
    MIN(ca.start_date) >= DATE_TRUNC('year', CURRENT_DATE);
