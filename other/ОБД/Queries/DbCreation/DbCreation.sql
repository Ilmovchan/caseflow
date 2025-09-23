CREATE TYPE detective_status AS ENUM (
  'Активний(а)',
  'У відпустці',
  'У відставці',
  'Звільнений(а)'
);

CREATE TYPE case_status AS ENUM (
  'Відкрито',
  'Закрито',
  'Призупинено'
);

CREATE TABLE "client" (
  "id" SERIAL PRIMARY KEY,
  "first_name" CHARACTER VARYING(100) NOT NULL,
  "last_name" CHARACTER VARYING(100) NOT NULL,
  "father_name" CHARACTER VARYING(100),
  "phone_number" CHARACTER VARYING(20) NOT NULL,
  "email" CHARACTER VARYING(100) NOT NULL,
  "date_of_birth" DATE NOT NULL,
  "region" CHARACTER VARYING(30) NOT NULL,
  "city" CHARACTER VARYING(30) NOT NULL,
  "street" CHARACTER VARYING(50) NOT NULL,
  "building_number" CHARACTER VARYING(30) NOT NULL,
  "apartment_number" INTEGER,
  "registration_date" DATE DEFAULT CURRENT_DATE,
  
  CONSTRAINT "name_format" CHECK (
    first_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND 
    last_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND 
    (father_name IS NULL OR father_name ~ '^[А-ЯІЇЄа-яіїє]+$')
  ),
  CONSTRAINT "phone_number_format" CHECK (phone_number ~ '^\+380\d{9}$'),
  CONSTRAINT "email_format" CHECK (email ~ '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'),
  CONSTRAINT "date_of_birth_format" CHECK (date_of_birth <= CURRENT_DATE),
  CONSTRAINT "region_format" CHECK (region ~ '^[А-ЯІЇЄа-яіїє]+$'),
  CONSTRAINT "city_format" CHECK (city ~ '^[А-ЯІЇЄа-яіїє\-]+$'),
  CONSTRAINT "street_format" CHECK (street ~ '^[А-ЯІЇЄа-яіїє\s\-]+$'),
  CONSTRAINT "building_number_format" CHECK (building_number ~ '^[0-9\/]+$'),
  CONSTRAINT "apartment_number_format" CHECK (apartment_number IS NULL OR apartment_number > 0)
);


CREATE TABLE "detective" (
  "id" SERIAL PRIMARY KEY,
  "first_name" CHARACTER VARYING(100) NOT NULL,
  "last_name" CHARACTER VARYING(100) NOT NULL,
  "father_name" CHARACTER VARYING(100),
  "phone_number" CHARACTER VARYING(20) NOT NULL,
  "email" CHARACTER VARYING(100) NOT NULL,
  "date_of_birth" DATE NOT NULL,
  "region" CHARACTER VARYING(30) NOT NULL,
  "city" CHARACTER VARYING(30) NOT NULL,
  "street" CHARACTER VARYING(50) NOT NULL,
  "building_number" CHARACTER VARYING(30) NOT NULL,
  "apartment_number" INTEGER,
  "hire_date" DATE NOT NULL,
  "salary" NUMERIC(10, 2) NOT NULL,
  "personal_notes" TEXT,
  "status" detective_status NOT NULL DEFAULT 'Активний(а)',
  
  CONSTRAINT "name_format" CHECK (
    first_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND 
    last_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND 
    (father_name IS NULL OR father_name ~ '^[А-ЯІЇЄа-яіїє]+$')
  ),
  CONSTRAINT "phone_number_format" CHECK (phone_number ~ '^\+380\d{9}$'),
  CONSTRAINT "email_format" CHECK (email ~ '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'),
  CONSTRAINT "date_of_birth_format" CHECK (date_of_birth <= CURRENT_DATE),
  CONSTRAINT "region_format" CHECK (region ~ '^[А-ЯІЇЄа-яіїє]+$'),
  CONSTRAINT "city_format" CHECK (city ~ '^[А-ЯІЇЄа-яіїє\-]+$'),
  CONSTRAINT "street_format" CHECK (street ~ '^[А-ЯІЇЄа-яіїє\s\-]+$'),
  CONSTRAINT "building_number_format" CHECK (building_number ~ '^[0-9\/]+$'),
  CONSTRAINT "apartment_number_format" CHECK (apartment_number IS NULL OR apartment_number > 0),
  CONSTRAINT "detective_hire_date_format" CHECK (hire_date <= CURRENT_DATE),
  CONSTRAINT "detective_salary_format" CHECK (salary >= 0)
);


CREATE TABLE "suspect" (
  "id" SERIAL PRIMARY KEY,
  "first_name" CHARACTER VARYING(100),
  "last_name" CHARACTER VARYING(100),
  "father_name" CHARACTER VARYING(100),
  "nickname" CHARACTER VARYING(50),
  "phone_number" CHARACTER VARYING(20),
  "date_of_birth" DATE,
  "region" CHARACTER VARYING(30),
  "city" CHARACTER VARYING(30),
  "street" CHARACTER VARYING(50),
  "building_number" CHARACTER VARYING(30),
  "apartment_number" INTEGER,
  "height" INTEGER,
  "weight" INTEGER,
  "physical_description" TEXT,
  "prior_convictions" TEXT,
  
  CONSTRAINT "name_format" CHECK (
    (first_name IS NULL OR first_name ~ '^[А-ЯІЇЄа-яіїє]+$') AND 
    (last_name IS NULL OR last_name ~ '^[А-ЯІЇЄа-яіїє]+$') AND 
    (father_name IS NULL OR father_name ~ '^[А-ЯІЇЄа-яіїє]+$') AND 
    (nickname IS NULL OR nickname ~ '^[А-ЯІЇЄа-яіїє0-9\s\-\.,:;\/]+$')
  ),
  CONSTRAINT "phone_number_format" CHECK (phone_number IS NULL OR phone_number ~ '^\+380\d{9}$'),
  CONSTRAINT "date_of_birth_format" CHECK (date_of_birth IS NULL OR date_of_birth <= CURRENT_DATE),
  CONSTRAINT "region_format" CHECK (region IS NULL OR region ~ '^[А-ЯІЇЄа-яіїє]+$'),
  CONSTRAINT "city_format" CHECK (city IS NULL OR city ~ '^[А-ЯІЇЄа-яіїє\-]+$'),
  CONSTRAINT "street_format" CHECK (street ~ '^[А-ЯІЇЄа-яіїє\s\-]+$'),
  CONSTRAINT "building_number_format" CHECK (building_number IS NULL OR building_number ~ '^[0-9\/]+$'),
  CONSTRAINT "apartment_number_format" CHECK (apartment_number IS NULL OR apartment_number > 0),
  CONSTRAINT "weight_height_format" CHECK (
    ((weight IS NOT NULL AND weight > 0) AND (height IS NOT NULL AND height > 0)) 
    OR (weight IS NULL AND height IS NULL)
  )
);


CREATE TABLE "case_type" (
  "id" SERIAL PRIMARY KEY,
  "name" CHARACTER VARYING(100) NOT NULL,
  "price" NUMERIC(10, 2) NOT NULL,
  
  CONSTRAINT "name_format" CHECK (name ~ '^[А-ЯІЇЄа-яіїє0-9\s\-\.,:;\/]+$'),
  CONSTRAINT "price_format" CHECK (price > 0)
);

CREATE TABLE "case" (
  "id" SERIAL PRIMARY KEY,
  "client_id" INTEGER NOT NULL,
  "detective_id" INTEGER,
  "case_type_id" INTEGER NOT NULL,
  "title" CHARACTER VARYING(255) NOT NULL,
  "description" TEXT NOT NULL,
  "start_date" DATE NOT NULL DEFAULT CURRENT_DATE,
  "deadline_date" DATE NOT NULL,
  "close_date" DATE,
  "status" case_status NOT NULL DEFAULT 'Відкрито',

  CONSTRAINT "deadline_format" CHECK (deadline_date >= start_date),
  CONSTRAINT "close_date_format" CHECK (close_date IS NULL OR close_date >= start_date),
  CONSTRAINT "FK_case_client_id" FOREIGN KEY ("client_id") REFERENCES "client" ("id"),
  CONSTRAINT "FK_case_detective_id" FOREIGN KEY ("detective_id") REFERENCES "detective" ("id"),
  CONSTRAINT "FK_case_case_type_id" FOREIGN KEY ("case_type_id") REFERENCES "case_type" ("id")
);


CREATE TABLE "evidence" (
  "id" SERIAL PRIMARY KEY,
  "description" TEXT NOT NULL,
  "type" CHARACTER VARYING(100) NOT NULL,
  "collection_date" DATE NOT NULL,

  CONSTRAINT "type_format" CHECK (type ~ '^[А-ЯІЇЄа-яіїє0-9 .,!?;:-]+$'),
  CONSTRAINT "collection_date_format" CHECK (collection_date <= CURRENT_DATE)
);


CREATE TABLE "case_evidence" (
  "evidence_id" INTEGER NOT NULL,
  "case_id" INTEGER NOT NULL,
  
  PRIMARY KEY ("evidence_id", "case_id"),
  CONSTRAINT "FK_case_evidence_case_id" FOREIGN KEY ("case_id") REFERENCES "case" ("id"),
  CONSTRAINT "FK_case_evidence_evidence_id" FOREIGN KEY ("evidence_id") REFERENCES "evidence" ("id")
);


CREATE TABLE "case_suspect" (
  "suspect_id" INTEGER NOT NULL,
  "case_id" INTEGER NOT NULL,
  "is_interrogated" BOOLEAN NOT NULL,
  "alibi" TEXT,
  
  PRIMARY KEY ("suspect_id", "case_id"),
  CONSTRAINT "FK_case_suspect_case_id" FOREIGN KEY ("case_id") REFERENCES "case" ("id"),
  CONSTRAINT "FK_case_suspect_suspect_id" FOREIGN KEY ("suspect_id") REFERENCES "suspect" ("id")
);


CREATE TABLE "report" (
  "id" SERIAL PRIMARY KEY,
  "case_id" INTEGER NOT NULL,
  "report_date" DATE DEFAULT CURRENT_DATE,
  "summary" TEXT NOT NULL,
  "comments" TEXT,
  
  CONSTRAINT "date_format" CHECK (report_date <= CURRENT_DATE),
  CONSTRAINT "FK_report_case_id" FOREIGN KEY ("case_id") REFERENCES "case" ("id")
);


CREATE TABLE "expense" (
  "id" SERIAL PRIMARY KEY,
  "case_id" INTEGER NOT NULL,
  "date_time" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "purpose" CHARACTER VARYING(255) NOT NULL,
  "amount" NUMERIC(10,2) NOT NULL,
  "annotation" TEXT,
  
  CONSTRAINT "date_time_format" CHECK (date_time <= CURRENT_TIMESTAMP),
  CONSTRAINT "purpose_format" CHECK (purpose ~ '^[А-ЯІЇЄа-яіїєA-Za-z0-9\s\-\.,:;]+$'),
  CONSTRAINT "amount_format" CHECK (amount > 0),
  CONSTRAINT "expense_case_id_fk" FOREIGN KEY ("case_id") REFERENCES "case" ("id")
);

