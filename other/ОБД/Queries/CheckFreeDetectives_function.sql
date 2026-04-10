SELECT d.id, 
       d.first_name, 
       d.last_name 
FROM public.detective d
LEFT JOIN public."case" c ON d.id = c.detective_id AND c.status = 'Відкрито'
WHERE c.id IS NULL;
