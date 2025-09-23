SELECT 
    detective_name, 
    RANK() OVER (ORDER BY CAST(closed_case_percentage AS NUMERIC) DESC) AS rank_closed_cases,
    closed_case_percentage || ' %' AS closed_cases_percent,
    RANK() OVER (ORDER BY overdue_cases_count DESC) AS rank_overdue_cases,
    overdue_cases_count AS total_overdue_cases,
    RANK() OVER (ORDER BY total_case_cost DESC) AS rank_case_cost,
    total_case_cost AS total_case_cost
FROM 
    detective_ranking
ORDER BY 
    rank_closed_cases;
