SELECT 
    c.first_name || ' ' || c.last_name AS client_name,
    d.first_name || ' ' || d.last_name AS detective_name,
    ct.name AS case_type,
    ca.title AS case_title,
    ca.description AS case_description,
    ca.start_date,
    ca.close_date - ca.start_date AS investigation_duration
FROM 
    "case" ca
JOIN 
    "client" c ON ca.client_id = c.id
LEFT JOIN 
    "detective" d ON ca.detective_id = d.id
JOIN 
    "case_type" ct ON ca.case_type_id = ct.id
WHERE 
    ca.status = 'Закрито'
    AND ca.close_date BETWEEN '2023-10-01' AND '2024-09-01';
