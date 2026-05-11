CREATE POLICY case_rls_detective_select
ON public."case"
FOR SELECT
TO PUBLIC
USING (
    pg_has_role(CURRENT_USER, 'detective', 'member')
    AND detective_id = cf_current_detective_id()
);