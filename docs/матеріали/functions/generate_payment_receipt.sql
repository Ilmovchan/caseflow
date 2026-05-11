CREATE OR REPLACE PROCEDURE generate_payment_receipt(
    p_case_id INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_case_title VARCHAR(255);
    v_case_type_name VARCHAR(100);
    v_case_type_price NUMERIC(10, 2);
    v_client_full_name VARCHAR(255);
    v_additional_expenses NUMERIC(10, 2);
    v_total_amount NUMERIC(10, 2);
    v_notice_text TEXT;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "case" WHERE id = p_case_id) THEN
        RAISE EXCEPTION 'Справу з ID % не знайдено.', p_case_id;
    END IF;
    
    SELECT c.title, ct.name, ct.price, 
           (cl.first_name || ' ' || cl.last_name)
    INTO v_case_title, v_case_type_name, v_case_type_price, v_client_full_name
    FROM "case" c
    JOIN case_type ct ON c.case_type_id = ct.id
    JOIN client cl ON c.client_id = cl.id
    WHERE c.id = p_case_id;
    
    SELECT COALESCE(SUM(amount), 0.00) 
    INTO v_additional_expenses
    FROM expense
    WHERE case_id = p_case_id;
    
    v_total_amount := v_case_type_price + v_additional_expenses;
    
    RAISE NOTICE 'Загальна сума до сплати: % грн.', v_total_amount;
END;
$$;
