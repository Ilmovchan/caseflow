SELECT 
    d.first_name || ' ' || d.last_name AS detective_name,
    c.first_name || ' ' || c.last_name AS client_name,
    COUNT(ca.id) AS total_cases
FROM 
    "case" ca
JOIN 
    "detective" d ON ca.detective_id = d.id
JOIN 
    "client" c ON ca.client_id = c.id
WHERE 
    ca.start_date >= CURRENT_DATE - INTERVAL '3 years'
    AND ca.status = 'Закрито'
GROUP BY 
    d.first_name, d.last_name, c.first_name, c.last_name
HAVING 
    COUNT(ca.id) >= 2
ORDER BY 
    total_cases DESC;
