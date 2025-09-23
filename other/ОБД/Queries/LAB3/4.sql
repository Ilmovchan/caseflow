SELECT 
    ca.id AS case_id,
    ca.title AS case_title,
    ca.description AS case_description,
    c.first_name || ' ' || c.last_name AS client_name,
    d.first_name || ' ' || d.last_name AS detective_name,
    s.first_name || ' ' || s.last_name AS suspect_name,
    s.physical_description AS suspect_physical_description,
    s.city AS suspect_city
FROM 
    "case" ca
JOIN 
    "case_suspect" cs ON ca.id = cs.case_id
JOIN 
    "suspect" s ON cs.suspect_id = s.id
JOIN 
    "client" c ON ca.client_id = c.id
LEFT JOIN 
    "detective" d ON ca.detective_id = d.id
WHERE 
    s.physical_description ILIKE '%тату%' 
    AND (cs.alibi IS NULL OR cs.alibi = '') 
    AND s.city = 'Київ';
