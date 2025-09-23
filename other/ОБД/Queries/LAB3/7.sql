SELECT 
    ca.id AS case_id,
    ca.title AS case_title,
    ca.description AS case_description,
    d.first_name || ' ' || d.last_name AS detective_name,
    c.first_name || ' ' || c.last_name AS client_name,
    ca.start_date,
    ca.close_date,
    ca.close_date - ca.start_date AS investigation_duration
FROM 
    "case" ca
LEFT JOIN 
    "detective" d ON ca.detective_id = d.id
JOIN 
    "client" c ON ca.client_id = c.id
WHERE 
    ca.close_date IS NOT NULL
ORDER BY 
    investigation_duration DESC
LIMIT 5;
