CREATE OR REPLACE PROCEDURE reassign_case(det_id INTEGER) AS $$
DECLARE
    current_case_id INTEGER;
    current_case_type_id INTEGER;
    new_detective_id INTEGER;
BEGIN
    SELECT id, case_type_id INTO current_case_id, current_case_type_id
    FROM "case"
    WHERE detective_id = det_id AND status = 'Відкрито'
    ORDER BY start_date DESC
    LIMIT 1;
    
    IF current_case_id IS NULL THEN
        RAISE NOTICE 'У детектива % немає відкритих справ.', det_id;
        RETURN;
    END IF;
    
    SELECT detective_id INTO new_detective_id
    FROM "case"
    WHERE case_type_id = current_case_type_id
        AND detective_id IS NOT NULL
        AND detective_id <> det_id
    ORDER BY close_date DESC NULLS LAST
    LIMIT 1;
    
    IF new_detective_id IS NOT NULL THEN
        UPDATE "case"
        SET detective_id = new_detective_id
        WHERE id = current_case_id;
        RAISE NOTICE 'Справу % передано детективу %.', current_case_id, new_detective_id;
    ELSE
        UPDATE "case"
        SET status = 'Призупинено'
        WHERE id = current_case_id;
        RAISE NOTICE 'Справу % переведено в статус "Призупинено", оскільки немає відповідних детективів.', current_case_id;
    END IF;
END;
$$ LANGUAGE plpgsql;