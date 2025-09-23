SELECT 
    RANK() OVER (ORDER BY SUM(e.amount) DESC) AS rating_position,
	SUM(e.amount) AS total_expenses,
    ca.id AS case_id,
    ca.title AS case_title,
    ca.description AS case_description
FROM 
    "case" ca
JOIN 
    "expense" e ON ca.id = e.case_id
WHERE 
    ca.status = 'Закрито'
GROUP BY 
    ca.id, ca.title, ca.description
ORDER BY 
    rating_position;
