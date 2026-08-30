-- =========================================================================
-- Seed Demo Data for Barangay Management System (MySQL)
-- Only inserts if data doesn't already exist (safe for re-runs)
-- =========================================================================

-- Barangay
INSERT INTO barangay (barangay_id, name)
SELECT 1, 'Barangay San Jose' FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM barangay WHERE barangay_id = 1);

UPDATE barangay SET name = 'Barangay San Jose' WHERE barangay_id = 1;

-- Roles
INSERT IGNORE INTO role (role_id, name, description) VALUES
(1, 'Super Admin', 'Primary system owner'),
(2, 'Admin', 'System administrator'),
(3, 'Staff', 'Staff account');

-- Puroks
INSERT IGNORE INTO purok_sitio (purok_id, barangay_id, name, type) VALUES
(1, 1, 'Purok 1 - Sampaguita', 'PUROK'),
(2, 1, 'Purok 2 - Rosal', 'PUROK'),
(3, 1, 'Purok 3 - Ilang-Ilang', 'PUROK'),
(4, 1, 'Purok 4 - Gumamela', 'PUROK'),
(5, 1, 'Purok 5 - Santan', 'PUROK'),
(6, 1, 'Purok 6 - Orchid', 'PUROK');

-- Households
INSERT IGNORE INTO household (household_id, barangay_id, purok_id, house_no, street) VALUES
(1, 1, 1, '101', 'Rizal Street'),
(2, 1, 1, '102', 'Rizal Street'),
(3, 1, 2, '201', 'Mabini Avenue'),
(4, 1, 2, '205', 'Mabini Avenue'),
(5, 1, 3, '301', 'Bonifacio Drive'),
(6, 1, 3, '310', 'Bonifacio Drive'),
(7, 1, 4, '401', 'Luna Street'),
(8, 1, 4, '415', 'Luna Street'),
(9, 1, 5, '501', 'Del Pilar Road'),
(10, 1, 6, '601', 'Aguinaldo Boulevard');

-- Residents (30 demo records)
INSERT IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter, is_head_of_family) VALUES
(1, 1, 1, 1, 'Juan', 'Santos', 'Dela Cruz', NULL, 'M', '1985-03-15', 'MARRIED', '09171234567', 'ACTIVE', 1, 1),
(4, 1, 1, 2, 'Roberto', 'Garcia', 'Santos', NULL, 'M', '1978-01-05', 'MARRIED', '09191234569', 'ACTIVE', 1, 1),
(6, 1, 2, 3, 'Carlos', 'Mendoza', 'Reyes', 'Jr.', 'M', '1965-09-30', 'WIDOWED', '09211234571', 'ACTIVE', 1, 1),
(8, 1, 3, 5, 'Miguel', 'Torres', 'Villanueva', NULL, 'M', '1982-04-12', 'MARRIED', '09231234573', 'ACTIVE', 1, 1),
(11, 1, 4, 7, 'Emmanuel', 'Cruz', 'Pascual', NULL, 'M', '1975-06-08', 'MARRIED', '09261234576', 'ACTIVE', 1, 1),
(14, 1, 5, 9, 'Antonio', 'Salazar', 'Mercado', NULL, 'M', '1980-11-19', 'MARRIED', '09291234579', 'ACTIVE', 1, 1),
(16, 1, 6, 10, 'Fernando', 'Aguilar', 'Soriano', NULL, 'M', '1972-09-05', 'MARRIED', '09311234581', 'ACTIVE', 1, 1),
(20, 1, 3, 6, 'Benjamin', 'Rivera', 'Ocampo', 'Sr.', 'M', '1968-05-11', 'MARRIED', '09351234585', 'ACTIVE', 1, 1),
(29, 1, 6, 10, 'Danilo', 'Enriquez', 'Villanueva', NULL, 'M', '1976-03-08', 'MARRIED', '09431234593', 'ACTIVE', 1, 1);

INSERT IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_registered_voter) VALUES
(2, 1, 1, 1, 'Maria', 'Reyes', 'Dela Cruz', NULL, 'F', '1987-07-22', 'MARRIED', '09181234568', 'ACTIVE', 1),
(9, 1, 3, 5, 'Teresa', 'Ramos', 'Villanueva', NULL, 'F', '1984-08-20', 'MARRIED', '09241234574', 'ACTIVE', 1),
(12, 1, 4, 7, 'Gloria', 'Navarro', 'Pascual', NULL, 'F', '1977-10-03', 'MARRIED', '09271234577', 'ACTIVE', 1),
(15, 1, 5, 9, 'Josephine', 'Castillo', 'Mercado', NULL, 'F', '1983-07-14', 'MARRIED', '09301234580', 'ACTIVE', 1),
(19, 1, 2, 4, 'Angelica', 'Flores', 'Bautista', NULL, 'F', '1992-01-17', 'SINGLE', '09341234584', 'ACTIVE', 1),
(23, 1, 6, 10, 'Patricia', 'Aguilar', 'Soriano', NULL, 'F', '1995-06-19', 'SINGLE', '09371234587', 'ACTIVE', 1),
(25, 1, 2, 3, 'Maricel', 'Lopez', 'Tan', NULL, 'F', '1991-04-03', 'MARRIED', '09391234589', 'ACTIVE', 1),
(27, 1, 4, 7, 'Christine', 'Cruz', 'Pascual', NULL, 'F', '2000-09-25', 'SINGLE', '09411234591', 'ACTIVE', 1);

INSERT IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_youth) VALUES
(3, 1, 1, 1, 'Pedro', 'Santos', 'Dela Cruz', NULL, 'M', '2005-11-10', 'SINGLE', NULL, 'ACTIVE', 1),
(18, 1, 1, 2, 'Mark', 'Santos', 'Garcia', NULL, 'M', '2002-08-30', 'SINGLE', '09331234583', 'ACTIVE', 1),
(22, 1, 5, 9, 'Kevin', 'Castillo', 'Mercado', NULL, 'M', '2004-02-28', 'SINGLE', NULL, 'ACTIVE', 1),
(30, 1, 2, 4, 'Jasmine', 'Bautista', 'Garcia', NULL, 'F', '2003-12-12', 'SINGLE', '09441234594', 'ACTIVE', 1);

INSERT IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_solo_parent) VALUES
(5, 1, 2, 3, 'Ana', 'Lopez', 'Reyes', NULL, 'F', '1990-05-18', 'SINGLE', '09201234570', 'ACTIVE', 1),
(28, 1, 5, 9, 'Rowena', 'Salazar', 'Diaz', NULL, 'F', '1989-11-30', 'SEPARATED', '09421234592', 'ACTIVE', 1);

INSERT IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_senior, is_registered_voter) VALUES
(7, 1, 2, 4, 'Lourdes', 'Bautista', 'Garcia', NULL, 'F', '1955-12-25', 'MARRIED', '09221234572', 'ACTIVE', 1, 1),
(17, 1, 6, 10, 'Corazon', 'Enriquez', 'Soriano', NULL, 'F', '1950-04-22', 'MARRIED', '09321234582', 'ACTIVE', 1, 0);

INSERT IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_pwd, is_registered_voter) VALUES
(10, 1, 3, 6, 'Ricardo', 'Aquino', 'Fernandez', NULL, 'M', '1970-02-14', 'MARRIED', '09251234575', 'ACTIVE', 1, 1),
(21, 1, 4, 8, 'Dolores', 'Magno', 'Santiago', NULL, 'F', '1960-12-01', 'WIDOWED', '09361234586', 'ACTIVE', 1, 0);

INSERT IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_4ps_beneficiary, is_indigent) VALUES
(13, 1, 4, 8, 'Rosario', 'Dizon', 'Manalo', NULL, 'F', '1988-03-27', 'SINGLE', '09281234578', 'ACTIVE', 1, 1),
(24, 1, 1, 1, 'Ernesto', 'Dela Cruz', 'Ramos', NULL, 'M', '1986-10-07', 'MARRIED', '09381234588', 'ACTIVE', 1, 1);

INSERT IGNORE INTO resident (resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name, suffix, sex, birth_date, civil_status, contact_no, status, is_senior, is_pwd) VALUES
(26, 1, 3, 5, 'Alfredo', 'Torres', 'Mendoza', NULL, 'M', '1948-07-16', 'WIDOWED', '09401234590', 'ACTIVE', 1, 1);

-- Barangay Officials
INSERT IGNORE INTO barangay_official (official_id, barangay_id, resident_id, full_name, position, is_active, sort_order) VALUES
(1, 1, 4, 'Roberto Garcia Santos', 'Punong Barangay', 1, 1),
(2, 1, 11, 'Emmanuel Cruz Pascual', 'Kagawad', 1, 2),
(3, 1, 8, 'Miguel Torres Villanueva', 'Kagawad', 1, 3),
(4, 1, 16, 'Fernando Aguilar Soriano', 'Kagawad', 1, 4),
(5, 1, 14, 'Antonio Salazar Mercado', 'Kagawad', 1, 5),
(6, 1, 20, 'Benjamin Rivera Ocampo Sr.', 'Kagawad', 1, 6),
(7, 1, 29, 'Danilo Enriquez Villanueva', 'Kagawad', 1, 7),
(8, 1, 6, 'Carlos Mendoza Reyes Jr.', 'SK Chairperson', 1, 8),
(9, 1, 9, 'Teresa Ramos Villanueva', 'Secretary', 1, 9),
(10, 1, 12, 'Gloria Navarro Pascual', 'Treasurer', 1, 10);

-- Ayuda Programs
INSERT IGNORE INTO ayuda_program (program_id, barangay_id, program_name, category, allocated_budget, status, start_date, end_date, notes) VALUES
(1, 1, 'SAP Cash Assistance 2026', 'Financial Assistance', 500000.00, 'ACTIVE', '2026-01-01', '2026-12-31', 'Social Amelioration Program for qualified residents'),
(2, 1, 'Senior Citizen Medical Aid', 'Medical Support', 200000.00, 'ACTIVE', '2026-01-01', '2026-06-30', 'Medical assistance for senior citizens'),
(3, 1, 'Back to School Supplies', 'Education Support', 150000.00, 'ACTIVE', '2026-05-01', '2026-08-31', 'School supplies for indigent students');

-- Default facilities
INSERT INTO barangay_facility (facility_name, facility_type, capacity, hourly_rate, location, is_active)
SELECT 'Barangay Hall', 'VENUE', 100, 0.00, 'Main Building', 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM barangay_facility WHERE facility_name = 'Barangay Hall');

INSERT INTO barangay_facility (facility_name, facility_type, capacity, hourly_rate, location, is_active)
SELECT 'Covered Court', 'VENUE', 300, 0.00, 'Plaza Area', 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM barangay_facility WHERE facility_name = 'Covered Court');

INSERT INTO barangay_facility (facility_name, facility_type, capacity, hourly_rate, location, is_active)
SELECT 'Multi-Purpose Hall', 'VENUE', 150, 0.00, 'Community Center', 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM barangay_facility WHERE facility_name = 'Multi-Purpose Hall');

-- Emergency contacts
INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'POLICE', 'Philippine National Police (PNP) Emergency', '911', 1, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='POLICE' AND agency_name='Philippine National Police (PNP) Emergency');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'FIRE', 'Bureau of Fire Protection (BFP)', '160', 1, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='FIRE' AND agency_name='Bureau of Fire Protection (BFP)');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'MEDICAL', 'Red Cross 143 Emergency Hotline', '143', 1, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='MEDICAL' AND agency_name='Red Cross 143 Emergency Hotline');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'DISASTER', 'NDRRMC Emergency Operations Center', '(02) 8911-1406', 1, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='DISASTER' AND agency_name='NDRRMC Emergency Operations Center');

-- System config
INSERT IGNORE INTO system_config (config_key, config_value) VALUES
('system_name', 'Barangay Management System'),
('barangay_name', 'Barangay San Jose'),
('municipality', 'Davao City'),
('province', 'Davao del Sur');

-- Default Super Admin account (username: Admin1, password: Admin1)
INSERT INTO user_account (user_id, barangay_id, username, password_hash, full_name, first_name, last_name, position, is_active)
SELECT 1, 1, 'Admin1', 'v1.100000.1hn1dRXACZcc0WR5sKdcxg==.s+GJQabxTnDtLXFqeEXYP+IaCfy1f4ebV4NKS6fohr8=', 'System Administrator', 'Admin', 'User', 'System Administrator', 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM user_account WHERE username = 'Admin1');

INSERT IGNORE INTO user_role (user_id, role_id) VALUES (1, 1);
