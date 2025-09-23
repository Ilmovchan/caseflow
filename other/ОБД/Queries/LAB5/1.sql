CREATE OR REPLACE FUNCTION check_detective_availability()
RETURNS TRIGGER AS $$
DECLARE
    current_case_deadline DATE;
BEGIN
    SELECT ca.deadline_date
    INTO current_case_deadline
    FROM "case" ca
    WHERE ca.detective_id = NEW.detective_id
      AND ca.status != 'Закрито'
      AND ca.start_date <= CURRENT_DATE
      AND (ca.close_date IS NULL OR ca.close_date >= CURRENT_DATE)
    ORDER BY ca.deadline_date ASC
    LIMIT 1;
    
    IF current_case_deadline IS NOT NULL THEN
        RAISE EXCEPTION 'Детектив вже зайнятий іншою справою. Він може звільнитися після %', current_case_deadline;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
