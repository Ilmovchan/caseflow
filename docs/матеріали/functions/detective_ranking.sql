CREATE OR REPLACE VIEW detective_ranking AS
SELECT 
    d.id AS detective_id,
    d.first_name || ' ' || d.last_name AS detective_name,
    ROUND(
        COUNT(*) FILTER (
            WHERE ca.status = 'Закрито'
              AND ca.start_date >= DATE_TRUNC('year', CURRENT_DATE)
        ) * 100.0
        / NULLIF(
            COUNT(*) FILTER (
                WHERE ca.start_date >= DATE_TRUNC('year', CURRENT_DATE)
            ),
            0
        ),
        2
    ) AS closed_case_percentage,
    COUNT(*) FILTER (
        WHERE ca.deadline_date < ca.close_date
          AND ca.start_date >= DATE_TRUNC('year', CURRENT_DATE)
    ) AS overdue_cases_count,
    (SELECT average_case_cost FROM get_average_case_cost(d.id)) AS total_case_cost,
    COUNT(DISTINCT ce.evidence_id) FILTER (
        WHERE ca.start_date >= DATE_TRUNC('year', CURRENT_DATE)
    ) AS total_evidence_count,
    COUNT(DISTINCT cs.suspect_id) FILTER (
        WHERE ca.start_date >= DATE_TRUNC('year', CURRENT_DATE)
    ) AS total_suspects_count
FROM 
    "detective" d
LEFT JOIN 
    "case" ca ON d.id = ca.detective_id
LEFT JOIN 
    "case_evidence" ce ON ca.id = ce.case_id
LEFT JOIN 
    "case_suspect" cs ON ca.id = cs.case_id
GROUP BY 
    d.id, d.first_name, d.last_name; 
