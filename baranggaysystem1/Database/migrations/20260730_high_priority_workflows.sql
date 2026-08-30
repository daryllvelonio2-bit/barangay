-- High-priority lifecycle hardening.
-- SchemaGuard adds the household/role archive columns provider-safely.

CREATE TABLE IF NOT EXISTS resident_death_record (
    death_record_id INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    resident_id INT NOT NULL,
    date_of_death DATE NOT NULL,
    place_of_death VARCHAR(255) NULL,
    cause_of_death VARCHAR(255) NULL,
    certificate_reference VARCHAR(120) NOT NULL,
    reported_by VARCHAR(150) NOT NULL,
    notes TEXT NULL,
    record_status VARCHAR(20) NOT NULL DEFAULT 'CONFIRMED',
    confirmed_by_user_id INT NULL,
    confirmed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reversal_reason VARCHAR(255) NULL,
    reversed_by_user_id INT NULL,
    reversed_at DATETIME NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_death_record_barangay_status (barangay_id, record_status),
    INDEX idx_death_record_resident (resident_id, record_status),
    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,
    FOREIGN KEY (resident_id) REFERENCES resident(resident_id) ON DELETE RESTRICT,
    FOREIGN KEY (confirmed_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,
    FOREIGN KEY (reversed_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);
