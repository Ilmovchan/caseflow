-- ========================
-- 1. Создаём базовые роли
-- ========================
CREATE ROLE admin NOINHERIT;
CREATE ROLE detective NOINHERIT;

-- Эти роли "групповые" — им мы даём привилегии,
-- а потом будем создавать конкретных пользователей
-- и назначать их в эти роли.

-- =========================
-- 2. Доступ администраторам
-- =========================
-- Полный доступ на все таблицы
GRANT ALL PRIVILEGES ON DATABASE "DetectiveAgencyDb" TO admin;
GRANT USAGE ON SCHEMA public TO admin;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO admin;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO admin;

-- Чтобы новые таблицы тоже были доступны админам автоматически
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO admin;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO admin;

-- ========================
-- 3. Доступ детективам (RLS)
-- ========================
-- Детективам разрешаем только SELECT/INSERT/UPDATE/DELETE,
-- но без "superuser"-прав
GRANT CONNECT ON DATABASE "DetectiveAgencyDb" TO detective;
GRANT USAGE ON SCHEMA public TO detective;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO detective;
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO detective;

-- Чтобы новые таблицы тоже были доступны
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO detective;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO detective;

-- =========================
-- 4. Включаем RLS для таблиц
-- =========================
ALTER TABLE public."case" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.case_evidence ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.case_suspect ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.expense ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.report ENABLE ROW LEVEL SECURITY;

-- =========================
-- 5. Политики для RLS
-- =========================
-- Каждому детективу доступны только свои дела
CREATE POLICY detective_cases_policy ON public."case"
    FOR ALL
    TO detective
    USING (detective_id = current_setting('app.current_detective_id')::int);

-- Доказательства только по своим делам
CREATE POLICY detective_evidence_policy ON public.case_evidence
    FOR ALL
    TO detective
    USING (case_id IN (SELECT id FROM public."case"
                       WHERE detective_id = current_setting('app.current_detective_id')::int));

-- Подозреваемые только по своим делам
CREATE POLICY detective_suspects_policy ON public.case_suspect
    FOR ALL
    TO detective
    USING (case_id IN (SELECT id FROM public."case"
                       WHERE detective_id = current_setting('app.current_detective_id')::int));

-- Траты только по своим делам
CREATE POLICY detective_expenses_policy ON public.expense
    FOR ALL
    TO detective
    USING (case_id IN (SELECT id FROM public."case"
                       WHERE detective_id = current_setting('app.current_detective_id')::int));

-- Отчёты только по своим делам
CREATE POLICY detective_reports_policy ON public.report
    FOR ALL
    TO detective
    USING (case_id IN (SELECT id FROM public."case"
                       WHERE detective_id = current_setting('app.current_detective_id')::int));
