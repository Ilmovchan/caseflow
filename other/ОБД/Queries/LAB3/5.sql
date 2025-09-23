SELECT 
    d.first_name || ' ' || d.last_name AS detective_name,
    ROUND(AVG(ca.close_date - ca.start_date), 2) AS average_case_duration
FROM 
    "case" ca
JOIN 
    "detective" d ON ca.detective_id = d.id
WHERE 
    ca.status = 'Закрито' 
    AND ca.close_date >= CURRENT_DATE - INTERVAL '1 year'
GROUP BY 
    d.first_name, d.last_name
ORDER BY 
    average_case_duration DESC;
