-- Schema Compatibility Test: Checks all columns referenced by the application
-- Run against barangay_system database to find missing columns/tables
USE barangay_system;

SET @db = DATABASE();
SET @missing = '';

-- Helper: Check if a column exists, append to @missing if not
-- We'll use a series of SELECT statements that report missing items

SELECT GROUP_CONCAT(CONCAT(tbl, '.', col) SEPARATOR '\n') AS missing_columns FROM (
  -- household
  SELECT 'household' AS tbl, 'address_note' AS col WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='household' AND COLUMN_NAME='address_note')
  UNION ALL SELECT 'household', 'latitude' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='household' AND COLUMN_NAME='latitude')
  UNION ALL SELECT 'household', 'longitude' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='household' AND COLUMN_NAME='longitude')
  -- resident
  UNION ALL SELECT 'resident', 'date_registered' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='resident' AND COLUMN_NAME='date_registered')
  UNION ALL SELECT 'resident', 'email' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='resident' AND COLUMN_NAME='email')
  -- case_record
  UNION ALL SELECT 'case_record', 'summary' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='case_record' AND COLUMN_NAME='summary')
  UNION ALL SELECT 'case_record', 'handled_by_user_id' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='case_record' AND COLUMN_NAME='handled_by_user_id')
  -- barangay_official (needs term_id, committee, status columns for new schema)
  UNION ALL SELECT 'barangay_official', 'term_id' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='barangay_official' AND COLUMN_NAME='term_id')
  UNION ALL SELECT 'barangay_official', 'committee' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='barangay_official' AND COLUMN_NAME='committee')
  UNION ALL SELECT 'barangay_official', 'status' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='barangay_official' AND COLUMN_NAME='status')
  -- official_term table
  UNION ALL SELECT 'official_term', '(TABLE)' WHERE NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='official_term')
  -- GlobalSearch uses cr.summary
  UNION ALL SELECT 'case_record', 'date_filed' WHERE NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='case_record' AND COLUMN_NAME='date_filed')
) AS checks;
