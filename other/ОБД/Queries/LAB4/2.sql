CREATE OR REPLACE VIEW detective_ranking AS
SELECT 
    d.id AS detective_id, 
    d.first_name || ' ' || d.last_name AS detective_name,
    ROUND(COUNT(CASE WHEN ca.status = 'Закрито' AND ca.start_date >= DATE_TRUNC('year', CURRENT_DATE) THEN 1 END) * 100.0 / 
          NULLIF(COUNT(CASE WHEN ca.start_date >= DATE_TRUNC('year', CURRENT_DATE) THEN 1 END), 0)) AS closed_case_percentage,
    COUNT(CASE WHEN ca.deadline_date < ca.close_date AND ca.start_date >= DATE_TRUNC('year', CURRENT_DATE) THEN 1 END) AS overdue_cases_count,
    (SELECT average_case_cost FROM get_average_case_cost(d.id)) AS total_case_cost,
    COUNT(DISTINCT ce.evidence_id) AS total_evidence_count,
    COUNT(DISTINCT cs.suspect_id) AS total_suspects_count
FROM 
    "detective" d
LEFT JOIN 
    "case" ca ON d.id = ca.detective_id
LEFT JOIN 
    "case_evidence" ce ON ca.id = ce.case_id
LEFT JOIN 
    "case_suspect" cs ON ca.id = cs.case_id
WHERE 
    ca.start_date >= DATE_TRUNC('year', CURRENT_DATE)
GROUP BY 
    d.id, d.first_name, d.last_name;
