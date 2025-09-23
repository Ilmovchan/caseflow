SELECT 
    ct.name AS case_type,
    COUNT(ca.id) AS total_cases
FROM 
    "case" ca
JOIN 
    "case_type" ct ON ca.case_type_id = ct.id
WHERE 
    ca.start_date >= DATE_TRUNC('month', CURRENT_DATE) - INTERVAL '1 month' 
    AND ca.start_date < DATE_TRUNC('month', CURRENT_DATE)
GROUP BY 
    ct.name
ORDER BY 
    total_cases DESC;
