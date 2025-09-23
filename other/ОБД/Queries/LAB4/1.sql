CREATE OR REPLACE FUNCTION get_average_case_cost(
    _detective_id INTEGER
)
RETURNS TABLE (
    detective_name VARCHAR,
    average_case_cost NUMERIC(10, 2)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        d.first_name || ' ' || d.last_name AS detective_name,
        ROUND(AVG(cd.total_case_cost) AS average_case_cost, 2)
    FROM 
        (
            SELECT 
                ca.id AS case_id,
                ca.detective_id,
                ct.price + COALESCE(SUM(e.amount), 0) AS total_case_cost
            FROM 
                public."case" ca
            JOIN 
                public."case_type" ct ON ca.case_type_id = ct.id
            LEFT JOIN 
                public."expense" e ON ca.id = e.case_id
            WHERE 
                ca.detective_id = _detective_id
            GROUP BY 
                ca.id, ca.detective_id, ct.price
        ) AS cd
    JOIN 
        public."detective" d ON cd.detective_id = d.id
    WHERE 
        d.id = _detective_id
    GROUP BY 
        d.first_name, d.last_name;
END;
$$;
