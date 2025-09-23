SELECT 
    c.first_name || ' ' || c.last_name AS client_name,
    COUNT(ca.id) AS total_cases
FROM 
    "client" c
JOIN 
    "case" ca ON c.id = ca.client_id
WHERE 
    ca.start_date >= DATE_TRUNC('year', CURRENT_DATE) 
GROUP BY 
    c.first_name, c.last_name
HAVING 
    COUNT(ca.id) >= 3
ORDER BY 
    total_cases DESC;
