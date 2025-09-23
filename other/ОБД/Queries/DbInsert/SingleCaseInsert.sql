INSERT INTO "case" 
    (client_id, detective_id, case_type_id, title, description, start_date, deadline_date, close_date, status) 
VALUES 
    (2, 5, 3, 'Розслідування зникнення', 'Зникнення людини в місті Київ. Необхідне негайне розслідування.', CURRENT_DATE, CURRENT_DATE + INTERVAL '15 days', NULL, 'Відкрито');
