-- IMS App initial schema (for bundled MySQL first-run)
-- Order respects foreign key dependencies

CREATE TABLE IF NOT EXISTS `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `password` varchar(255) DEFAULT NULL,
  `role` enum('Master','User') DEFAULT NULL,
  `status` enum('1','0') NOT NULL DEFAULT '1',
  `vcode` varchar(200) DEFAULT NULL,
  `country` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `brands` (
  `brand_id` int NOT NULL AUTO_INCREMENT,
  `brand_name` varchar(50) NOT NULL,
  `b_status` enum('1','0') DEFAULT '0',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`brand_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `categories` (
  `cat_id` int NOT NULL AUTO_INCREMENT,
  `main_cat` int NOT NULL DEFAULT 0,
  `category_name` varchar(50) NOT NULL,
  `status` enum('1','0') NOT NULL DEFAULT '0',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`cat_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `suppliers` (
  `supplier_id` int NOT NULL AUTO_INCREMENT,
  `supplier_name` varchar(100) NOT NULL,
  `contact_person` varchar(100) DEFAULT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `address` text,
  `status` enum('1','0') DEFAULT '1',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`supplier_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `products` (
  `pid` int NOT NULL AUTO_INCREMENT,
  `cat_id` int DEFAULT NULL,
  `brand_id` int DEFAULT NULL,
  `supplier_id` int DEFAULT NULL,
  `product_name` varchar(50) NOT NULL,
  `stock` int NOT NULL DEFAULT 0,
  `price` double NOT NULL,
  `buying_price` double DEFAULT 0,
  `unit` varchar(20) DEFAULT 'pieces',
  `purchase_unit` varchar(20) DEFAULT NULL,
  `sale_unit` varchar(20) DEFAULT NULL,
  `conversion_factor` int DEFAULT 1,
  `unit_cost` decimal(10,2) DEFAULT NULL,
  `wholesale_price` decimal(10,2) DEFAULT NULL,
  `description` varchar(200) DEFAULT '',
  `p_status` enum('1','0') NOT NULL DEFAULT '1',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `expiry_date` date DEFAULT NULL,
  PRIMARY KEY (`pid`),
  KEY `cat_id` (`cat_id`),
  KEY `brand_id` (`brand_id`),
  KEY `supplier_id` (`supplier_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `orders` (
  `invoice_no` int NOT NULL AUTO_INCREMENT,
  `customer_name` varchar(100) NOT NULL,
  `address` varchar(255) NOT NULL DEFAULT '',
  `subtotal` double NOT NULL,
  `gst` double NOT NULL,
  `discount` double NOT NULL,
  `net_total` double NOT NULL,
  `paid` decimal(10,2) NOT NULL DEFAULT 0,
  `due` decimal(10,2) NOT NULL DEFAULT 0,
  `payment_method` varchar(50) NOT NULL DEFAULT 'Cash',
  `order_date` varchar(50) NOT NULL,
  PRIMARY KEY (`invoice_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `invoices` (
  `id` int NOT NULL AUTO_INCREMENT,
  `invoice_no` int NOT NULL,
  `product_name` varchar(100) NOT NULL,
  `order_qty` int NOT NULL,
  `price_per_item` double NOT NULL,
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `invoice_no` (`invoice_no`),
  CONSTRAINT `invoices_ibfk_1` FOREIGN KEY (`invoice_no`) REFERENCES `orders` (`invoice_no`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `customer_payments` (
  `id` int NOT NULL AUTO_INCREMENT,
  `invoice_no` int NOT NULL,
  `amount_paid` decimal(10,2) NOT NULL,
  `payment_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `payment_method` varchar(50) DEFAULT 'Cash',
  `notes` text,
  `created_by` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `invoice_no` (`invoice_no`),
  KEY `created_by` (`created_by`),
  CONSTRAINT `customer_payments_ibfk_1` FOREIGN KEY (`invoice_no`) REFERENCES `orders` (`invoice_no`) ON DELETE CASCADE,
  CONSTRAINT `customer_payments_ibfk_2` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `stock_reconciliations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `product_id` int NOT NULL,
  `system_stock` int NOT NULL,
  `physical_count` int NOT NULL,
  `difference` int NOT NULL,
  `reconciliation_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `status` enum('pending','approved','rejected') DEFAULT 'pending',
  `notes` text,
  `created_by` int DEFAULT NULL,
  `approved_by` int DEFAULT NULL,
  `approved_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `product_id` (`product_id`),
  KEY `created_by` (`created_by`),
  KEY `approved_by` (`approved_by`),
  CONSTRAINT `stock_reconciliations_ibfk_1` FOREIGN KEY (`product_id`) REFERENCES `products` (`pid`) ON DELETE CASCADE,
  CONSTRAINT `stock_reconciliations_ibfk_2` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `stock_reconciliations_ibfk_3` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `migrations` (
  `id` varchar(255) NOT NULL,
  `description` text,
  `applied_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `branding_settings` (
  `id` int NOT NULL AUTO_INCREMENT,
  `business_name` varchar(100) DEFAULT NULL,
  `business_address` varchar(255) DEFAULT NULL,
  `business_phone` varchar(50) DEFAULT NULL,
  `business_email` varchar(100) DEFAULT NULL,
  `currency_symbol` varchar(10) DEFAULT 'UGX',
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT IGNORE INTO users (name, email, password, role, status, country) VALUES
('Admin', 'admin@gmail.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Master', '1', 'Uganda');

INSERT IGNORE INTO branding_settings (id, business_name, business_address, business_phone, currency_symbol) VALUES
(1, 'My Business', '123 Main Street', '+256 700 000 000', 'UGX');
