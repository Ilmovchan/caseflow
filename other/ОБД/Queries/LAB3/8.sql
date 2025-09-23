SELECT 
    RANK() OVER (ORDER BY ROUND(COUNT(CASE WHEN ca.status != 'Закрито' THEN 1 END) * 100.0 / COUNT(ca.id)) DESC) AS rating_position,
	ROUND(COUNT(CASE WHEN ca.status != 'Закрито' THEN 1 END) * 100.0 / COUNT(ca.id)) || ' %' AS unclosed_case_percentage,
    d.first_name || ' ' || d.last_name AS detective_name
FROM 
    "detective" d
JOIN 
    "case" ca ON d.id = ca.detective_id
GROUP BY 
    d.first_name, d.last_name
ORDER BY 
    rating_position;
