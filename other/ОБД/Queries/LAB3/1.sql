SELECT 
    e.id AS evidence_id,
    e.description,
    e.type,
    e.collection_date
FROM 
    "evidence" e
JOIN 
    "case_evidence" ce ON e.id = ce.evidence_id
WHERE 
    ce.case_id = 1;
