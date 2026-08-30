USE barangay_system;

DELIMITER //

-- Keep doc_type_id in sync with document_type_id on document_type
CREATE TRIGGER trg_doctype_sync_insert BEFORE INSERT ON document_type
FOR EACH ROW
BEGIN
    IF NEW.doc_type_id IS NULL OR NEW.doc_type_id = 0 THEN
        SET NEW.doc_type_id = NEW.document_type_id;
    END IF;
END //

-- Keep doc_type_id in sync on document_request
CREATE TRIGGER trg_docrequest_sync_insert BEFORE INSERT ON document_request
FOR EACH ROW
BEGIN
    IF NEW.doc_type_id IS NULL AND NEW.document_type_id IS NOT NULL THEN
        SET NEW.doc_type_id = NEW.document_type_id;
    END IF;
END //

-- Keep case_no in sync with case_number on case_record
CREATE TRIGGER trg_case_no_sync_insert BEFORE INSERT ON case_record
FOR EACH ROW
BEGIN
    IF NEW.case_no IS NULL AND NEW.case_number IS NOT NULL THEN
        SET NEW.case_no = NEW.case_number;
    END IF;
END //

CREATE TRIGGER trg_case_no_sync_update BEFORE UPDATE ON case_record
FOR EACH ROW
BEGIN
    IF NEW.case_number IS NOT NULL AND (NEW.case_no IS NULL OR NEW.case_no != NEW.case_number) THEN
        SET NEW.case_no = NEW.case_number;
    END IF;
END //

DELIMITER ;
