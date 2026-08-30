-- Barangay Management System - SQLite Demo Seed Data
-- Populates the database with sample residents, households, and reference data.

-- Mark seed migrations as applied
INSERT OR IGNORE INTO schema_migrations (migration_name) VALUES ('20260309_seed_30_records_30_transactions_reports.sql');
INSERT OR IGNORE INTO schema_migrations (migration_name) VALUES ('20260428_ph_public_reference_seed.sql');
INSERT OR IGNORE INTO schema_migrations (migration_name) VALUES ('20260515_demo_seed.sql');

-- Ensure barangay exists
INSERT OR IGNORE INTO barangay (barangay_id, name) VALUES (1, 'Barangay San Jose');

-- Update barangay name
UPDATE barangay SET name = 'Barangay San Jose' WHERE barangay_id = 1;

-- Puroks
INSERT OR IGNORE INTO purok_sitio (purok_id, barangay_id, name, type) VALUES (1, 1, 'Purok 1 - Sampaguita', 'PUROK');
INSERT OR IGNORE INTO purok_sitio (purok_id, barangay_id, name, type) VALUES (2, 1, 'Purok 2 - Rosal', 'PUROK');
INSERT OR IGNORE INTO purok_sitio (purok_id, barangay_id, name, type) VALUES (3, 1, 'Purok 3 - Ilang-Ilang', 'PUROK');
INSERT OR IGNORE INTO purok_sitio (purok_id, barangay_id, name, type) VALUES (4, 1, 'Purok 4 - Gumamela', 'PUROK');
INSERT OR IGNORE INTO purok_sitio (purok_id, barangay_id, name, type) VALUES (5, 1, 'Purok 5 - Santan', 'PUROK');
INSERT OR IGNORE INTO purok_sitio (purok_id, barangay_id, name, type) VALUES (6, 1, 'Purok 6 - Orchid', 'PUROK');

-- Households
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (1, 1, 1, '101', 'Rizal Street');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (2, 1, 1, '102', 'Rizal Street');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (3, 1, 2, '201', 'Mabini Avenue');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (4, 1, 2, '205', 'Mabini Avenue');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (5, 1, 3, '301', 'Bonifacio Drive');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (6, 1, 3, '310', 'Bonifacio Drive');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (7, 1, 4, '401', 'Luna Street');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (8, 1, 4, '415', 'Luna Street');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (9, 1, 5, '501', 'Del Pilar Road');
INSERT OR IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES (10, 1, 6, '601', 'Aguinaldo Boulevard');

-- Residents (30 sample records)
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(1, 1, 1, 1, 'Juan', 'Santos', 'Dela Cruz', NULL, 'M', '1985-03-15', 'MARRIED', '09171234567', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(2, 1, 1, 1, 'Maria', 'Reyes', 'Dela Cruz', NULL, 'F', '1987-07-22', 'MARRIED', '09181234568', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_youth) VALUES
(3, 1, 1, 1, 'Pedro', 'Santos', 'Dela Cruz', NULL, 'M', '2005-11-10', 'SINGLE', NULL, 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(4, 1, 1, 2, 'Roberto', 'Garcia', 'Santos', NULL, 'M', '1978-01-05', 'MARRIED', '09191234569', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_solo_parent) VALUES
(5, 1, 2, 3, 'Ana', 'Lopez', 'Reyes', NULL, 'F', '1990-05-18', 'SINGLE', '09201234570', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(6, 1, 2, 3, 'Carlos', 'Mendoza', 'Reyes', 'Jr.', 'M', '1965-09-30', 'WIDOWED', '09211234571', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_senior, is_registered_voter) VALUES
(7, 1, 2, 4, 'Lourdes', 'Bautista', 'Garcia', NULL, 'F', '1955-12-25', 'MARRIED', '09221234572', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(8, 1, 3, 5, 'Miguel', 'Torres', 'Villanueva', NULL, 'M', '1982-04-12', 'MARRIED', '09231234573', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(9, 1, 3, 5, 'Teresa', 'Ramos', 'Villanueva', NULL, 'F', '1984-08-20', 'MARRIED', '09241234574', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_pwd, is_registered_voter) VALUES
(10, 1, 3, 6, 'Ricardo', 'Aquino', 'Fernandez', NULL, 'M', '1970-02-14', 'MARRIED', '09251234575', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(11, 1, 4, 7, 'Emmanuel', 'Cruz', 'Pascual', NULL, 'M', '1975-06-08', 'MARRIED', '09261234576', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(12, 1, 4, 7, 'Gloria', 'Navarro', 'Pascual', NULL, 'F', '1977-10-03', 'MARRIED', '09271234577', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_4ps_beneficiary, is_indigent) VALUES
(13, 1, 4, 8, 'Rosario', 'Dizon', 'Manalo', NULL, 'F', '1988-03-27', 'SINGLE', '09281234578', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(14, 1, 5, 9, 'Antonio', 'Salazar', 'Mercado', NULL, 'M', '1980-11-19', 'MARRIED', '09291234579', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(15, 1, 5, 9, 'Josephine', 'Castillo', 'Mercado', NULL, 'F', '1983-07-14', 'MARRIED', '09301234580', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(16, 1, 6, 10, 'Fernando', 'Aguilar', 'Soriano', NULL, 'M', '1972-09-05', 'MARRIED', '09311234581', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_senior) VALUES
(17, 1, 6, 10, 'Corazon', 'Enriquez', 'Soriano', NULL, 'F', '1950-04-22', 'MARRIED', '09321234582', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_youth) VALUES
(18, 1, 1, 2, 'Mark', 'Santos', 'Garcia', NULL, 'M', '2002-08-30', 'SINGLE', '09331234583', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(19, 1, 2, 4, 'Angelica', 'Flores', 'Bautista', NULL, 'F', '1992-01-17', 'SINGLE', '09341234584', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(20, 1, 3, 6, 'Benjamin', 'Rivera', 'Ocampo', 'Sr.', 'M', '1968-05-11', 'MARRIED', '09351234585', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_pwd) VALUES
(21, 1, 4, 8, 'Dolores', 'Magno', 'Santiago', NULL, 'F', '1960-12-01', 'WIDOWED', '09361234586', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_youth) VALUES
(22, 1, 5, 9, 'Kevin', 'Castillo', 'Mercado', NULL, 'M', '2004-02-28', 'SINGLE', NULL, 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(23, 1, 6, 10, 'Patricia', 'Aguilar', 'Soriano', NULL, 'F', '1995-06-19', 'SINGLE', '09371234587', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_4ps_beneficiary, is_indigent) VALUES
(24, 1, 1, 1, 'Ernesto', 'Dela Cruz', 'Ramos', NULL, 'M', '1986-10-07', 'MARRIED', '09381234588', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(25, 1, 2, 3, 'Maricel', 'Lopez', 'Tan', NULL, 'F', '1991-04-03', 'MARRIED', '09391234589', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_senior, is_pwd) VALUES
(26, 1, 3, 5, 'Alfredo', 'Torres', 'Mendoza', NULL, 'M', '1948-07-16', 'WIDOWED', '09401234590', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(27, 1, 4, 7, 'Christine', 'Cruz', 'Pascual', NULL, 'F', '2000-09-25', 'SINGLE', '09411234591', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_solo_parent) VALUES
(28, 1, 5, 9, 'Rowena', 'Salazar', 'Diaz', NULL, 'F', '1989-11-30', 'SEPARATED', '09421234592', 'ACTIVE', 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(29, 1, 6, 10, 'Danilo', 'Enriquez', 'Villanueva', NULL, 'M', '1976-03-08', 'MARRIED', '09431234593', 'ACTIVE', 1, 1);
INSERT OR IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_youth) VALUES
(30, 1, 2, 4, 'Jasmine', 'Bautista', 'Garcia', NULL, 'F', '2003-12-12', 'SINGLE', '09441234594', 'ACTIVE', 1);

-- System config
INSERT OR IGNORE INTO system_config (config_key, config_value) VALUES ('system_name', 'Barangay Management System');
INSERT OR IGNORE INTO system_config (config_key, config_value) VALUES ('barangay_name', 'Barangay San Jose');
INSERT OR IGNORE INTO system_config (config_key, config_value) VALUES ('municipality', 'Davao City');
INSERT OR IGNORE INTO system_config (config_key, config_value) VALUES ('province', 'Davao del Sur');

-- Ayuda Programs
INSERT OR IGNORE INTO ayuda_program (program_id, barangay_id, program_name, category, allocated_budget, status, start_date, end_date, notes) VALUES
(1, 1, 'SAP Cash Assistance 2026', 'Financial Assistance', 500000.00, 'ACTIVE', '2026-01-01', '2026-12-31', 'Social Amelioration Program for qualified residents');
INSERT OR IGNORE INTO ayuda_program (program_id, barangay_id, program_name, category, allocated_budget, status, start_date, end_date, notes) VALUES
(2, 1, 'Senior Citizen Medical Aid', 'Medical Support', 200000.00, 'ACTIVE', '2026-01-01', '2026-06-30', 'Medical assistance for senior citizens');
INSERT OR IGNORE INTO ayuda_program (program_id, barangay_id, program_name, category, allocated_budget, status, start_date, end_date, notes) VALUES
(3, 1, 'Back to School Supplies', 'Education Support', 150000.00, 'ACTIVE', '2026-05-01', '2026-08-31', 'School supplies for indigent students');

-- Barangay Officials
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(1, 1, 4, 'Roberto Garcia Santos', 'Punong Barangay', 1, 1);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(2, 1, 11, 'Emmanuel Cruz Pascual', 'Kagawad', 1, 2);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(3, 1, 8, 'Miguel Torres Villanueva', 'Kagawad', 1, 3);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(4, 1, 16, 'Fernando Aguilar Soriano', 'Kagawad', 1, 4);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(5, 1, 14, 'Antonio Salazar Mercado', 'Kagawad', 1, 5);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(6, 1, 20, 'Benjamin Rivera Ocampo Sr.', 'Kagawad', 1, 6);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(7, 1, 29, 'Danilo Enriquez Villanueva', 'Kagawad', 1, 7);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(8, 1, 6, 'Carlos Mendoza Reyes Jr.', 'SK Chairperson', 1, 8);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(9, 1, 9, 'Teresa Ramos Villanueva', 'Secretary', 1, 9);
INSERT OR IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(10, 1, 12, 'Gloria Navarro Pascual', 'Treasurer', 1, 10);

-- User account (default admin)
INSERT OR IGNORE INTO user_account (user_id, barangay_id, username, password_hash, full_name, first_name, last_name, position, is_active) VALUES
(1, 1, 'admin', '$2a$11$placeholder_hash_for_demo', 'Admin User', 'Admin', 'User', 'System Administrator', 1);
INSERT OR IGNORE INTO user_role (user_id, role_id) VALUES (1, 1);
