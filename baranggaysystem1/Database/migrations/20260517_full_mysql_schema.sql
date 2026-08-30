-- =========================================================================
-- Full MySQL Schema Bootstrap Migration
-- Creates all tables if they don't exist (safe for re-runs)
-- =========================================================================

-- Core tables (handled by SchemaGuard but included for completeness)
CREATE TABLE IF NOT EXISTS barangay (
    barangay_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS purok_sitio (
    purok_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL DEFAULT 'PUROK',
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    latitude DECIMAL(10,8) NULL,
    longitude DECIMAL(11,8) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_purok_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS role (
    role_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(500),
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS user_account (
    user_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    username VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(255),
    first_name VARCHAR(100),
    middle_name VARCHAR(100),
    last_name VARCHAR(100),
    email VARCHAR(255),
    contact_no VARCHAR(50),
    position VARCHAR(100),
    department VARCHAR(100),
    photo_url VARCHAR(500),
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    must_change_password TINYINT NOT NULL DEFAULT 0,
    last_login_at DATETIME,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_user_account_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS user_role (
    user_role_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_user_role_user FOREIGN KEY (user_id) REFERENCES user_account (user_id) ON DELETE CASCADE,
    CONSTRAINT fk_user_role_role FOREIGN KEY (role_id) REFERENCES role (role_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS role_permission (
    role_permission_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    role_id INT NOT NULL,
    permission_key VARCHAR(100) NOT NULL,
    is_allowed TINYINT(1) NOT NULL DEFAULT 0,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_role_permission_role FOREIGN KEY (role_id) REFERENCES role (role_id) ON DELETE CASCADE,
    CONSTRAINT ux_role_permission UNIQUE (role_id, permission_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS household (
    household_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    purok_id INT NOT NULL,
    house_no VARCHAR(50),
    street VARCHAR(255),
    subdivision VARCHAR(255),
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_household_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_household_purok FOREIGN KEY (purok_id) REFERENCES purok_sitio (purok_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS resident (
    resident_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    purok_id INT NOT NULL DEFAULT 1,
    household_id INT,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    last_name VARCHAR(100) NOT NULL,
    suffix VARCHAR(20),
    sex CHAR(1) NOT NULL DEFAULT 'M',
    birth_date DATE,
    civil_status VARCHAR(30),
    contact_no VARCHAR(50),
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    photo LONGBLOB,
    photo_url VARCHAR(500),
    education_level VARCHAR(100),
    occupation VARCHAR(100),
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    is_senior TINYINT(1) NOT NULL DEFAULT 0,
    is_pwd TINYINT(1) NOT NULL DEFAULT 0,
    is_4ps_beneficiary TINYINT(1) NOT NULL DEFAULT 0,
    is_registered_voter TINYINT(1) NOT NULL DEFAULT 0,
    is_head_of_family TINYINT(1) NOT NULL DEFAULT 0,
    is_solo_parent TINYINT(1) NOT NULL DEFAULT 0,
    is_youth TINYINT(1) NOT NULL DEFAULT 0,
    is_indigent TINYINT(1) NOT NULL DEFAULT 0,
    deleted_at DATETIME,
    deleted_by_user_id INT,
    delete_reason TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_resident_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_resident_purok FOREIGN KEY (purok_id) REFERENCES purok_sitio (purok_id) ON DELETE CASCADE,
    CONSTRAINT fk_resident_household FOREIGN KEY (household_id) REFERENCES household (household_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS document_type (
    document_type_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    doc_type_id INT NULL,
    name VARCHAR(255) NOT NULL,
    code VARCHAR(20),
    requires_approval TINYINT(1) NOT NULL DEFAULT 1,
    validity_days INT DEFAULT 365,
    renewal_reminder_days INT DEFAULT 30,
    fee_default DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS certificate (
    certificate_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    resident_id INT NOT NULL,
    document_type_id INT NOT NULL,
    or_number VARCHAR(50),
    purpose TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    issued_at DATETIME,
    expires_at DATETIME,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_certificate_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_certificate_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE,
    CONSTRAINT fk_certificate_doctype FOREIGN KEY (document_type_id) REFERENCES document_type (document_type_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS case_type (
    case_type_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS case_record (
    case_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    case_type_id INT,
    complainant_id INT,
    respondent_id INT,
    respondent_resident_id INT,
    respondent_name VARCHAR(255),
    case_number VARCHAR(50),
    case_no VARCHAR(50),
    date_filed DATETIME NULL DEFAULT CURRENT_TIMESTAMP,
    incident_date DATE,
    incident_location VARCHAR(500),
    incident_type VARCHAR(100),
    incident_time TIME,
    incident_details TEXT,
    narrative TEXT,
    witness_names TEXT,
    action_taken TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'OPEN',
    resolution TEXT,
    resolution_details TEXT,
    referral_destination VARCHAR(255),
    closure_notes TEXT,
    resolved_at DATETIME,
    closed_at DATETIME,
    closed_by_user_id INT,
    recorded_by INT,
    ai_summary TEXT,
    ai_key_points TEXT,
    ai_category VARCHAR(150),
    ai_category_confidence DECIMAL(5,4),
    ai_risk_level VARCHAR(20),
    ai_risk_score INT,
    ai_risk_reasons TEXT,
    ai_entities TEXT,
    ai_recommended_next_action TEXT,
    ai_model VARCHAR(100),
    ai_processed_at DATETIME,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_case_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_case_type FOREIGN KEY (case_type_id) REFERENCES case_type (case_type_id) ON DELETE SET NULL,
    CONSTRAINT fk_case_complainant FOREIGN KEY (complainant_id) REFERENCES resident (resident_id) ON DELETE SET NULL,
    CONSTRAINT fk_case_respondent FOREIGN KEY (respondent_id) REFERENCES resident (resident_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS case_hearing (
    hearing_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    case_id INT NOT NULL,
    schedule_at DATETIME,
    venue VARCHAR(255),
    status VARCHAR(20) NOT NULL DEFAULT 'SCHEDULED',
    minutes TEXT,
    result TEXT,
    created_by_user_id INT,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_hearing_case FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS case_timeline (
    timeline_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    case_id INT NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    event_title VARCHAR(255) NOT NULL,
    event_details TEXT,
    from_status VARCHAR(50),
    to_status VARCHAR(50),
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INT,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_timeline_case FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS resident_transfer_history (
    transfer_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    resident_id INT NOT NULL,
    old_purok_id INT,
    old_household_id INT,
    old_address VARCHAR(500),
    new_purok_id INT,
    new_household_id INT,
    new_address VARCHAR(500),
    transfer_reason TEXT,
    transferred_by_user_id INT,
    transferred_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_transfer_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS audit_trail (
    audit_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    module VARCHAR(60) NOT NULL DEFAULT '',
    entity_type VARCHAR(60) NOT NULL DEFAULT '',
    entity_id VARCHAR(64) NULL,
    action VARCHAR(60) NOT NULL DEFAULT '',
    before_json LONGTEXT NULL,
    after_json LONGTEXT NULL,
    notes TEXT NULL,
    action_by INT NULL,
    action_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    INDEX idx_audit_entity (entity_type, entity_id),
    INDEX idx_audit_module (module),
    INDEX idx_audit_action_at (action_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS projects (
    project_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    name VARCHAR(255),
    description TEXT,
    category VARCHAR(100),
    status VARCHAR(30) NOT NULL DEFAULT 'PLANNED',
    start_date DATE,
    end_date DATE,
    budget DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    record_type VARCHAR(50) NOT NULL DEFAULT 'Project',
    attendance_target INT NOT NULL DEFAULT 0,
    attendance_count INT NOT NULL DEFAULT 0,
    last_activity_date DATE,
    outcome_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    outcome_summary TEXT,
    lead VARCHAR(255),
    remarks TEXT,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_projects_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS collection_entry (
    collection_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    resident_id INT,
    collection_type VARCHAR(100) NOT NULL,
    amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    or_number VARCHAR(50),
    payment_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    remarks TEXT,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_collection_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_collection_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS announcement (
    announcement_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT,
    category VARCHAR(100),
    priority VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    is_published TINYINT(1) NOT NULL DEFAULT 0,
    published_at DATETIME,
    expires_at DATETIME,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_announcement_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS announcements (
    announcement_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    title VARCHAR(255) NOT NULL,
    body TEXT,
    priority VARCHAR(20) NOT NULL DEFAULT 'Normal',
    status VARCHAR(20) NOT NULL DEFAULT 'Published',
    is_pinned TINYINT(1) NOT NULL DEFAULT 0,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS system_config (
    config_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    config_key VARCHAR(100) NOT NULL UNIQUE,
    config_value TEXT,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS schema_migrations (
    migration_name VARCHAR(255) NOT NULL PRIMARY KEY,
    applied_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS offline_sync_queue (
    queue_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    sql_text TEXT NOT NULL,
    parameters_json TEXT,
    queued_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'pending'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS sync_queue (
    queue_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL DEFAULT 'unknown',
    operation VARCHAR(50) NOT NULL DEFAULT 'UNKNOWN',
    sql_text TEXT NOT NULL,
    parameter_json TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    dedupe_key VARCHAR(255) UNIQUE,
    retry_count INT NOT NULL DEFAULT 0,
    last_error TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS barangay_official (
    official_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    resident_id INT,
    full_name VARCHAR(255) NOT NULL,
    position VARCHAR(100) NOT NULL,
    term_start DATE,
    term_end DATE,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    sort_order INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_official_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS notification_outbox (
    notification_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    recipient_type VARCHAR(30) NOT NULL DEFAULT 'RESIDENT',
    recipient_id INT,
    channel VARCHAR(20) NOT NULL DEFAULT 'SMS',
    subject VARCHAR(255),
    body TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'QUEUED',
    sent_at DATETIME,
    error_message TEXT,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS outbound_notification (
    notification_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    dedupe_key VARCHAR(255) UNIQUE,
    channel VARCHAR(30) NOT NULL,
    recipient VARCHAR(255) NOT NULL,
    subject VARCHAR(255),
    message TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    source_module VARCHAR(100),
    source_record_id INT,
    template_key VARCHAR(100),
    scheduled_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sent_at DATETIME,
    attempts INT NOT NULL DEFAULT 0,
    last_error TEXT,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS outbound_notification_attempt (
    attempt_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    notification_id INT NOT NULL,
    attempt_no INT NOT NULL,
    attempted_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    success TINYINT(1) NOT NULL DEFAULT 0,
    response_code VARCHAR(50),
    response_message TEXT,
    CONSTRAINT fk_attempt_notification FOREIGN KEY (notification_id) REFERENCES outbound_notification (notification_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- GOVERNANCE: MEETINGS & RESOLUTIONS
-- =========================================================================

CREATE TABLE IF NOT EXISTS barangay_meeting (
    meeting_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    meeting_type VARCHAR(50) NOT NULL DEFAULT 'REGULAR',
    title VARCHAR(255) NOT NULL,
    scheduled_at DATETIME NOT NULL,
    venue VARCHAR(255),
    agenda TEXT,
    minutes TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'SCHEDULED',
    attendance_count INT NOT NULL DEFAULT 0,
    quorum_reached TINYINT(1) NOT NULL DEFAULT 0,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS meeting_attendance (
    attendance_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    meeting_id INT NOT NULL,
    official_id INT,
    attendee_name VARCHAR(255) NOT NULL,
    position VARCHAR(100),
    is_present TINYINT(1) NOT NULL DEFAULT 1,
    remarks TEXT,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_attendance_meeting FOREIGN KEY (meeting_id) REFERENCES barangay_meeting (meeting_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS barangay_resolution (
    resolution_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    meeting_id INT,
    document_type VARCHAR(50) NOT NULL DEFAULT 'RESOLUTION',
    document_number VARCHAR(50) NOT NULL,
    series_year INT NOT NULL,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    full_text LONGTEXT,
    effectivity_date DATE,
    expiration_date DATE,
    status VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    authored_by VARCHAR(255),
    approved_by VARCHAR(255),
    approved_at DATETIME,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_resolution_meeting FOREIGN KEY (meeting_id) REFERENCES barangay_meeting (meeting_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- FACILITY BOOKING
-- =========================================================================

CREATE TABLE IF NOT EXISTS barangay_facility (
    facility_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    facility_name VARCHAR(255) NOT NULL,
    facility_type VARCHAR(50) NOT NULL DEFAULT 'VENUE',
    capacity INT,
    hourly_rate DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    location VARCHAR(255),
    description TEXT,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS facility_booking (
    booking_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    facility_id INT NOT NULL,
    resident_id INT,
    requester_name VARCHAR(255) NOT NULL,
    requester_contact VARCHAR(100),
    purpose TEXT NOT NULL,
    start_at DATETIME NOT NULL,
    end_at DATETIME NOT NULL,
    expected_guests INT,
    total_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    payment_status VARCHAR(20) NOT NULL DEFAULT 'UNPAID',
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    approved_by_user_id INT,
    approved_at DATETIME,
    cancellation_reason TEXT,
    remarks TEXT,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_booking_facility FOREIGN KEY (facility_id) REFERENCES barangay_facility (facility_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- TANOD PATROL SCHEDULER
-- =========================================================================

CREATE TABLE IF NOT EXISTS tanod_member (
    tanod_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    resident_id INT,
    full_name VARCHAR(255) NOT NULL,
    contact_number VARCHAR(50),
    rank_title VARCHAR(100),
    date_assigned DATE,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    remarks TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS tanod_shift (
    shift_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    shift_date DATE NOT NULL,
    shift_type VARCHAR(30) NOT NULL DEFAULT 'MORNING',
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    area_assignment VARCHAR(255),
    notes TEXT,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS tanod_shift_assignment (
    assignment_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    shift_id INT NOT NULL,
    tanod_id INT NOT NULL,
    attendance_status VARCHAR(30) NOT NULL DEFAULT 'SCHEDULED',
    check_in_at DATETIME,
    check_out_at DATETIME,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    UNIQUE KEY uq_shift_tanod (shift_id, tanod_id),
    CONSTRAINT fk_assignment_shift FOREIGN KEY (shift_id) REFERENCES tanod_shift (shift_id) ON DELETE CASCADE,
    CONSTRAINT fk_assignment_tanod FOREIGN KEY (tanod_id) REFERENCES tanod_member (tanod_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS tanod_patrol_log (
    log_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    shift_id INT,
    barangay_id INT NOT NULL DEFAULT 1,
    logged_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    location VARCHAR(255),
    incident_type VARCHAR(100),
    description TEXT NOT NULL,
    severity VARCHAR(20) NOT NULL DEFAULT 'LOW',
    action_taken TEXT,
    reported_by VARCHAR(255),
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_patrol_shift FOREIGN KEY (shift_id) REFERENCES tanod_shift (shift_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- DOCUMENT REQUEST
-- =========================================================================

CREATE TABLE IF NOT EXISTS document_request (
    doc_request_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    resident_id INT,
    document_type_id INT,
    doc_type_id INT NULL,
    document_no VARCHAR(50),
    status VARCHAR(30) NOT NULL DEFAULT 'SUBMITTED',
    purpose TEXT,
    fee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    or_number VARCHAR(50),
    business_name VARCHAR(255),
    business_nature VARCHAR(255),
    verification_token VARCHAR(255),
    verification_token_created_at DATETIME,
    expires_at DATETIME,
    renewed_from_request_id INT,
    renewal_notified_at DATETIME,
    release_notified_at DATETIME,
    print_count INT NOT NULL DEFAULT 0,
    last_printed_at DATETIME,
    remarks TEXT,
    requested_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    approved_at DATETIME,
    released_at DATETIME,
    cancelled_at DATETIME,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS document_payment (
    payment_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    doc_request_id INT NOT NULL,
    amount DECIMAL(10,2),
    or_no VARCHAR(50),
    payment_method VARCHAR(50),
    paid_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    received_by_user_id INT,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- RECORD ATTACHMENTS & BACKUPS
-- =========================================================================

CREATE TABLE IF NOT EXISTS record_attachment (
    attachment_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    entity_type VARCHAR(100) NOT NULL,
    entity_id INT NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_ext VARCHAR(20),
    mime_type VARCHAR(100),
    file_size_bytes INT NOT NULL DEFAULT 0,
    file_hash VARCHAR(128),
    file_blob LONGBLOB NOT NULL,
    notes TEXT,
    uploaded_by_user_id INT,
    uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS backup_run (
    backup_run_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at DATETIME,
    status VARCHAR(20) NOT NULL DEFAULT 'RUNNING',
    backup_type VARCHAR(30) NOT NULL DEFAULT 'FULL',
    base_started_at DATETIME,
    base_backup_run_id INT,
    file_path VARCHAR(500),
    file_size_bytes BIGINT,
    error_message TEXT,
    created_by_user_id INT,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- EMERGENCY CONTACTS
-- =========================================================================

CREATE TABLE IF NOT EXISTS emergency_contact (
    contact_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    category VARCHAR(50) NOT NULL DEFAULT 'OTHER',
    agency_name VARCHAR(255) NOT NULL,
    contact_person VARCHAR(255),
    phone_primary VARCHAR(50) NOT NULL,
    phone_secondary VARCHAR(50),
    email VARCHAR(255),
    address TEXT,
    notes TEXT,
    is_priority TINYINT(1) NOT NULL DEFAULT 0,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- AYUDA ASSISTANCE MODULE
-- =========================================================================

CREATE TABLE IF NOT EXISTS ayuda_program (
    program_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    program_name VARCHAR(255) NOT NULL,
    category VARCHAR(100) NOT NULL DEFAULT 'Financial Assistance',
    allocated_budget DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    start_date DATE,
    end_date DATE,
    notes TEXT,
    created_by_user_id INT,
    updated_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ayuda_release_batch (
    batch_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    program_id INT NOT NULL,
    batch_reference VARCHAR(100) NOT NULL,
    release_date DATE NOT NULL,
    total_amount DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    beneficiary_count INT NOT NULL DEFAULT 0,
    notes TEXT,
    report_file_path VARCHAR(500),
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_batch_program FOREIGN KEY (program_id) REFERENCES ayuda_program (program_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ayuda_release (
    release_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    program_id INT NOT NULL,
    batch_id INT,
    resident_id INT NOT NULL,
    batch_reference VARCHAR(100),
    reference_no VARCHAR(100) NOT NULL,
    amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    released_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    release_status VARCHAR(20) NOT NULL DEFAULT 'RELEASED',
    notes TEXT,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_release_program FOREIGN KEY (program_id) REFERENCES ayuda_program (program_id) ON DELETE CASCADE,
    CONSTRAINT fk_release_batch FOREIGN KEY (batch_id) REFERENCES ayuda_release_batch (batch_id) ON DELETE SET NULL,
    CONSTRAINT fk_release_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- PAYMENT LEDGER
-- =========================================================================

CREATE TABLE IF NOT EXISTS payment_ledger (
    payment_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL DEFAULT 1,
    resident_id INT NOT NULL,
    resident_name VARCHAR(255) NOT NULL,
    amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    or_number VARCHAR(50) NOT NULL,
    payment_method VARCHAR(50) NOT NULL DEFAULT 'Cash',
    remarks TEXT,
    payment_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_ledger_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
