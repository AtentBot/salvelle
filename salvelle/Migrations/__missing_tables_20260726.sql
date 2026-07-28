-- DDL das 18 tabelas faltantes, extraido do modelo (schemagen) 2026-07-26
-- Idempotente. Aplicar no banco de producao salvelle.

-- FASE 1: CREATE TABLE
CREATE TABLE IF NOT EXISTS "RawMaterialsCatalog" (
  "Id" uuid NOT NULL,
  "Name" character varying(150) NOT NULL,
  "DcbCode" character varying(50),
  "CasNumber" character varying(50),
  "Category" character varying(100),
  "ControlType" character varying(20) NOT NULL,
  "AllowedUsage" character varying(20) NOT NULL,
  "PhysicalState" character varying(20) NOT NULL,
  "Unit" character varying(10) NOT NULL,
  "DefaultPurityFactor" numeric(10,4) NOT NULL,
  "DefaultCorrectionFactor" numeric(6,4) NOT NULL,
  "Synonyms" text,
  "Indications" text,
  "Popularity" integer NOT NULL,
  "IsActive" boolean NOT NULL,
  "CreatedAt" timestamp without time zone NOT NULL,
  "UpdatedAt" timestamp without time zone NOT NULL
);
CREATE TABLE IF NOT EXISTS "audit_logs" (
  "Id" uuid NOT NULL,
  "Action" character varying(100) NOT NULL,
  "EntityType" character varying(100),
  "EntityId" character varying(100),
  "Details" character varying(1000),
  "UserId" uuid,
  "UserType" character varying(20),
  "IpAddress" character varying(45),
  "EstablishmentId" uuid,
  "CreatedAt" timestamp without time zone NOT NULL
);
CREATE TABLE IF NOT EXISTS "carts" (
  "id" uuid NOT NULL,
  "customer_id" uuid NOT NULL,
  "establishment_id" uuid NOT NULL,
  "status" character varying(20) NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone NOT NULL
);
CREATE TABLE IF NOT EXISTS "cart_items" (
  "id" uuid NOT NULL,
  "session_token" character varying(100) NOT NULL,
  "customer_id" uuid,
  "establishment_id" uuid NOT NULL,
  "item_type" character varying(20) NOT NULL,
  "reference_id" uuid,
  "name" character varying(300) NOT NULL,
  "description" text,
  "quantity" integer NOT NULL,
  "unit_price" numeric NOT NULL,
  "total_price" numeric NOT NULL,
  "requires_prescription" boolean NOT NULL,
  "is_controlled" boolean NOT NULL,
  "is_custom_formula" boolean NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "expires_at" timestamp without time zone
);
CREATE TABLE IF NOT EXISTS "platform_commissions" (
  "id" uuid NOT NULL,
  "establishment_id" uuid NOT NULL,
  "week_start_date" timestamp without time zone NOT NULL,
  "week_end_date" timestamp without time zone NOT NULL,
  "total_sales_count" integer NOT NULL,
  "commission_rate" numeric(5,4) NOT NULL,
  "total_sales_amount" numeric(18,2) NOT NULL,
  "total_commission_amount" numeric(18,2) NOT NULL,
  "status" character varying(20) NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone
);
CREATE TABLE IF NOT EXISTS "platform_transactions" (
  "id" uuid NOT NULL,
  "order_id" uuid NOT NULL,
  "establishment_id" uuid NOT NULL,
  "customer_id" uuid,
  "gross_amount" numeric(18,2) NOT NULL,
  "commission_rate" numeric(5,4) NOT NULL,
  "commission_amount" numeric(18,2) NOT NULL,
  "net_amount_to_pharmacy" numeric(18,2) NOT NULL,
  "stripe_payment_intent_id" character varying(200),
  "stripe_transfer_id" character varying(200),
  "status" character varying(20) NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone
);
CREATE TABLE IF NOT EXISTS "pharmacy_ratings" (
  "id" uuid NOT NULL,
  "establishment_id" uuid NOT NULL,
  "customer_id" uuid NOT NULL,
  "order_id" uuid,
  "rating" integer NOT NULL,
  "comment" character varying(1000),
  "pharmacy_response" character varying(1000),
  "pharmacy_responded_at" timestamp without time zone,
  "created_at" timestamp without time zone NOT NULL
);
CREATE TABLE IF NOT EXISTS "product_ratings" (
  "id" uuid NOT NULL,
  "catalog_product_id" uuid NOT NULL,
  "customer_id" uuid NOT NULL,
  "order_id" uuid,
  "rating" integer NOT NULL,
  "comment" character varying(1000),
  "created_at" timestamp without time zone NOT NULL
);
CREATE TABLE IF NOT EXISTS "pharmacy_payout_accounts" (
  "id" uuid NOT NULL,
  "establishment_id" uuid NOT NULL,
  "stripe_connect_account_id" character varying(200),
  "bank_name" character varying(100),
  "agency_number" character varying(20),
  "account_number" character varying(30),
  "pix_key" character varying(200),
  "status" character varying(20) NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone
);
CREATE TABLE IF NOT EXISTS "delivery_estimates" (
  "id" uuid NOT NULL,
  "order_id" uuid NOT NULL,
  "estimated_minutes" integer NOT NULL,
  "estimated_delivery_at" timestamp without time zone NOT NULL,
  "actual_delivery_at" timestamp without time zone,
  "status" character varying(20) NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone
);
CREATE TABLE IF NOT EXISTS "formula_cart_items" (
  "id" uuid NOT NULL,
  "cart_id" uuid NOT NULL,
  "customer_formula_id" uuid,
  "catalog_product_id" uuid,
  "quantity" integer NOT NULL,
  "unit_price" numeric(10,2) NOT NULL,
  "notes" character varying(500),
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone NOT NULL
);
CREATE TABLE IF NOT EXISTS "customer_addresses" (
  "id" uuid NOT NULL,
  "customer_id" uuid NOT NULL,
  "label" character varying(50),
  "street" character varying(200) NOT NULL,
  "number" character varying(20) NOT NULL,
  "complement" character varying(100),
  "neighborhood" character varying(100) NOT NULL,
  "city" character varying(100) NOT NULL,
  "state" character varying(2) NOT NULL,
  "zip_code" character varying(8) NOT NULL,
  "latitude" double precision,
  "longitude" double precision,
  "is_default" boolean NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone
);
CREATE TABLE IF NOT EXISTS "customer_devices" (
  "id" uuid NOT NULL,
  "customer_id" uuid NOT NULL,
  "device_token" character varying(500) NOT NULL,
  "platform" character varying(10) NOT NULL,
  "device_model" character varying(100),
  "os_version" character varying(20),
  "app_version" character varying(20),
  "is_active" boolean NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone
);
CREATE TABLE IF NOT EXISTS "company_settings" (
  "id" uuid NOT NULL,
  "razao_social" character varying(200) NOT NULL,
  "nome_fantasia" character varying(200),
  "cnpj" character varying(18) NOT NULL,
  "inscricao_estadual" character varying(20),
  "inscricao_municipal" character varying(20),
  "logradouro" character varying(200),
  "numero" character varying(20),
  "complemento" character varying(100),
  "bairro" character varying(100),
  "cidade" character varying(100),
  "uf" character varying(2),
  "cep" character varying(9),
  "telefone" character varying(20),
  "celular" character varying(20),
  "email" character varying(200),
  "website" character varying(200),
  "alvara_sanitario" character varying(50),
  "alvara_validade" timestamp without time zone,
  "autorizacao_anvisa" character varying(50),
  "autorizacao_especial" character varying(50),
  "logo_base64" text,
  "logo_url" character varying(500),
  "prazo_validade_padrao_dias" integer NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "updated_at" timestamp without time zone
);
CREATE TABLE IF NOT EXISTS "label_print_logs" (
  "id" uuid NOT NULL,
  "generated_label_id" uuid NOT NULL,
  "printed_by_id" uuid NOT NULL,
  "printed_at" timestamp without time zone NOT NULL,
  "copies" integer NOT NULL,
  "format" character varying(10) NOT NULL,
  "printer_name" character varying(100),
  "print_reason" character varying(30) NOT NULL,
  "notes" character varying(500)
);
CREATE TABLE IF NOT EXISTS "pending_two_factor_sessions" (
  "id" uuid NOT NULL,
  "temp_token" character varying(128) NOT NULL,
  "employee_id" uuid NOT NULL,
  "expires_at" timestamp without time zone NOT NULL,
  "is_used" boolean NOT NULL,
  "created_at" timestamp without time zone NOT NULL,
  "ip_address" character varying(45)
);
CREATE TABLE IF NOT EXISTS "revoked_jwts" (
  "id" uuid NOT NULL,
  "jwt_id" character varying(128) NOT NULL,
  "expires_at" timestamp without time zone NOT NULL,
  "revoked_at" timestamp without time zone NOT NULL,
  "customer_id" uuid
);
CREATE TABLE IF NOT EXISTS "search_history" (
  "id" uuid NOT NULL,
  "customer_id" uuid,
  "search_term" character varying(300) NOT NULL,
  "latitude" double precision,
  "longitude" double precision,
  "result_count" integer NOT NULL,
  "search_type" character varying(20) NOT NULL,
  "created_at" timestamp without time zone NOT NULL
);

-- FASE 2: PK / UNIQUE
ALTER TABLE "RawMaterialsCatalog" ADD CONSTRAINT "PK_RawMaterialsCatalog" PRIMARY KEY ("Id");
ALTER TABLE "audit_logs" ADD CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id");
ALTER TABLE "carts" ADD CONSTRAINT "PK_carts" PRIMARY KEY (id);
ALTER TABLE "cart_items" ADD CONSTRAINT "PK_cart_items" PRIMARY KEY (id);
ALTER TABLE "platform_commissions" ADD CONSTRAINT "PK_platform_commissions" PRIMARY KEY (id);
ALTER TABLE "platform_transactions" ADD CONSTRAINT "PK_platform_transactions" PRIMARY KEY (id);
ALTER TABLE "pharmacy_ratings" ADD CONSTRAINT "PK_pharmacy_ratings" PRIMARY KEY (id);
ALTER TABLE "product_ratings" ADD CONSTRAINT "PK_product_ratings" PRIMARY KEY (id);
ALTER TABLE "pharmacy_payout_accounts" ADD CONSTRAINT "PK_pharmacy_payout_accounts" PRIMARY KEY (id);
ALTER TABLE "delivery_estimates" ADD CONSTRAINT "PK_delivery_estimates" PRIMARY KEY (id);
ALTER TABLE "formula_cart_items" ADD CONSTRAINT "PK_formula_cart_items" PRIMARY KEY (id);
ALTER TABLE "customer_addresses" ADD CONSTRAINT "PK_customer_addresses" PRIMARY KEY (id);
ALTER TABLE "customer_devices" ADD CONSTRAINT "PK_customer_devices" PRIMARY KEY (id);
ALTER TABLE "company_settings" ADD CONSTRAINT "PK_company_settings" PRIMARY KEY (id);
ALTER TABLE "label_print_logs" ADD CONSTRAINT "PK_label_print_logs" PRIMARY KEY (id);
ALTER TABLE "pending_two_factor_sessions" ADD CONSTRAINT "PK_pending_two_factor_sessions" PRIMARY KEY (id);
ALTER TABLE "revoked_jwts" ADD CONSTRAINT "PK_revoked_jwts" PRIMARY KEY (id);
ALTER TABLE "search_history" ADD CONSTRAINT "PK_search_history" PRIMARY KEY (id);

-- FASE 3: CHECK

-- FASE 4: FOREIGN KEYS
ALTER TABLE "carts" ADD CONSTRAINT "FK_carts_Establishments_establishment_id" FOREIGN KEY (establishment_id) REFERENCES "Establishments"("Id") ON DELETE CASCADE;
ALTER TABLE "carts" ADD CONSTRAINT "FK_carts_customers_customer_id" FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;
ALTER TABLE "platform_commissions" ADD CONSTRAINT "FK_platform_commissions_Establishments_establishment_id" FOREIGN KEY (establishment_id) REFERENCES "Establishments"("Id") ON DELETE RESTRICT;
ALTER TABLE "platform_transactions" ADD CONSTRAINT "FK_platform_transactions_Establishments_establishment_id" FOREIGN KEY (establishment_id) REFERENCES "Establishments"("Id") ON DELETE RESTRICT;
ALTER TABLE "platform_transactions" ADD CONSTRAINT "FK_platform_transactions_OnlineOrders_order_id" FOREIGN KEY (order_id) REFERENCES "OnlineOrders"("Id") ON DELETE RESTRICT;
ALTER TABLE "platform_transactions" ADD CONSTRAINT "FK_platform_transactions_customers_customer_id" FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL;
ALTER TABLE "pharmacy_ratings" ADD CONSTRAINT "FK_pharmacy_ratings_Establishments_establishment_id" FOREIGN KEY (establishment_id) REFERENCES "Establishments"("Id") ON DELETE CASCADE;
ALTER TABLE "pharmacy_ratings" ADD CONSTRAINT "FK_pharmacy_ratings_OnlineOrders_order_id" FOREIGN KEY (order_id) REFERENCES "OnlineOrders"("Id") ON DELETE SET NULL;
ALTER TABLE "pharmacy_ratings" ADD CONSTRAINT "FK_pharmacy_ratings_customers_customer_id" FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;
ALTER TABLE "product_ratings" ADD CONSTRAINT "FK_product_ratings_CatalogProducts_catalog_product_id" FOREIGN KEY (catalog_product_id) REFERENCES "CatalogProducts"("Id") ON DELETE CASCADE;
ALTER TABLE "product_ratings" ADD CONSTRAINT "FK_product_ratings_OnlineOrders_order_id" FOREIGN KEY (order_id) REFERENCES "OnlineOrders"("Id") ON DELETE SET NULL;
ALTER TABLE "product_ratings" ADD CONSTRAINT "FK_product_ratings_customers_customer_id" FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;
ALTER TABLE "pharmacy_payout_accounts" ADD CONSTRAINT "FK_pharmacy_payout_accounts_Establishments_establishment_id" FOREIGN KEY (establishment_id) REFERENCES "Establishments"("Id") ON DELETE CASCADE;
ALTER TABLE "delivery_estimates" ADD CONSTRAINT "FK_delivery_estimates_OnlineOrders_order_id" FOREIGN KEY (order_id) REFERENCES "OnlineOrders"("Id") ON DELETE CASCADE;
ALTER TABLE "formula_cart_items" ADD CONSTRAINT "FK_formula_cart_items_CatalogProducts_catalog_product_id" FOREIGN KEY (catalog_product_id) REFERENCES "CatalogProducts"("Id");
ALTER TABLE "formula_cart_items" ADD CONSTRAINT "FK_formula_cart_items_carts_cart_id" FOREIGN KEY (cart_id) REFERENCES carts(id) ON DELETE CASCADE;
ALTER TABLE "formula_cart_items" ADD CONSTRAINT "FK_formula_cart_items_customer_formulas_customer_formula_id" FOREIGN KEY (customer_formula_id) REFERENCES customer_formulas(id);
ALTER TABLE "customer_addresses" ADD CONSTRAINT "FK_customer_addresses_customers_customer_id" FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;
ALTER TABLE "customer_devices" ADD CONSTRAINT "FK_customer_devices_customers_customer_id" FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE;
ALTER TABLE "label_print_logs" ADD CONSTRAINT "FK_label_print_logs_employees_printed_by_id" FOREIGN KEY (printed_by_id) REFERENCES employees("Id") ON DELETE CASCADE;
ALTER TABLE "label_print_logs" ADD CONSTRAINT "FK_label_print_logs_generated_labels_generated_label_id" FOREIGN KEY (generated_label_id) REFERENCES generated_labels(id) ON DELETE CASCADE;
ALTER TABLE "pending_two_factor_sessions" ADD CONSTRAINT "FK_pending_two_factor_sessions_employees_employee_id" FOREIGN KEY (employee_id) REFERENCES employees("Id") ON DELETE CASCADE;
ALTER TABLE "search_history" ADD CONSTRAINT "FK_search_history_customers_customer_id" FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL;

-- FASE 5: INDEXES (nao-constraint)
CREATE INDEX IF NOT EXISTS "IX_RawMaterialsCatalog_AllowedUsage" ON public."RawMaterialsCatalog" USING btree ("AllowedUsage");
CREATE INDEX IF NOT EXISTS "IX_RawMaterialsCatalog_Category" ON public."RawMaterialsCatalog" USING btree ("Category");
CREATE INDEX IF NOT EXISTS "IX_RawMaterialsCatalog_DcbCode" ON public."RawMaterialsCatalog" USING btree ("DcbCode");
CREATE INDEX IF NOT EXISTS "IX_RawMaterialsCatalog_IsActive" ON public."RawMaterialsCatalog" USING btree ("IsActive");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RawMaterialsCatalog_Name" ON public."RawMaterialsCatalog" USING btree ("Name");
CREATE INDEX IF NOT EXISTS "IX_RawMaterialsCatalog_Popularity" ON public."RawMaterialsCatalog" USING btree ("Popularity");
CREATE INDEX IF NOT EXISTS "IX_carts_customer_id" ON public.carts USING btree (customer_id);
CREATE INDEX IF NOT EXISTS "IX_carts_establishment_id" ON public.carts USING btree (establishment_id);
CREATE INDEX IF NOT EXISTS "IX_cart_items_customer_id" ON public.cart_items USING btree (customer_id);
CREATE INDEX IF NOT EXISTS "IX_cart_items_establishment_id" ON public.cart_items USING btree (establishment_id);
CREATE INDEX IF NOT EXISTS "IX_cart_items_session_token" ON public.cart_items USING btree (session_token);
CREATE INDEX IF NOT EXISTS "IX_platform_commissions_establishment_id_week_start_date" ON public.platform_commissions USING btree (establishment_id, week_start_date);
CREATE INDEX IF NOT EXISTS "IX_platform_commissions_status" ON public.platform_commissions USING btree (status);
CREATE INDEX IF NOT EXISTS "IX_platform_transactions_customer_id" ON public.platform_transactions USING btree (customer_id);
CREATE INDEX IF NOT EXISTS "IX_platform_transactions_establishment_id" ON public.platform_transactions USING btree (establishment_id);
CREATE INDEX IF NOT EXISTS "IX_platform_transactions_order_id_status" ON public.platform_transactions USING btree (order_id, status);
CREATE INDEX IF NOT EXISTS "IX_platform_transactions_stripe_payment_intent_id" ON public.platform_transactions USING btree (stripe_payment_intent_id);
CREATE INDEX IF NOT EXISTS "IX_pharmacy_ratings_customer_id" ON public.pharmacy_ratings USING btree (customer_id);
CREATE INDEX IF NOT EXISTS "IX_pharmacy_ratings_establishment_id" ON public.pharmacy_ratings USING btree (establishment_id);
CREATE INDEX IF NOT EXISTS "IX_pharmacy_ratings_order_id" ON public.pharmacy_ratings USING btree (order_id);
CREATE INDEX IF NOT EXISTS "IX_product_ratings_catalog_product_id" ON public.product_ratings USING btree (catalog_product_id);
CREATE INDEX IF NOT EXISTS "IX_product_ratings_customer_id" ON public.product_ratings USING btree (customer_id);
CREATE INDEX IF NOT EXISTS "IX_product_ratings_order_id" ON public.product_ratings USING btree (order_id);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_pharmacy_payout_accounts_establishment_id" ON public.pharmacy_payout_accounts USING btree (establishment_id);
CREATE INDEX IF NOT EXISTS "IX_pharmacy_payout_accounts_stripe_connect_account_id" ON public.pharmacy_payout_accounts USING btree (stripe_connect_account_id);
CREATE INDEX IF NOT EXISTS "IX_delivery_estimates_order_id" ON public.delivery_estimates USING btree (order_id);
CREATE INDEX IF NOT EXISTS "IX_delivery_estimates_status" ON public.delivery_estimates USING btree (status);
CREATE INDEX IF NOT EXISTS "IX_formula_cart_items_cart_id" ON public.formula_cart_items USING btree (cart_id);
CREATE INDEX IF NOT EXISTS "IX_formula_cart_items_catalog_product_id" ON public.formula_cart_items USING btree (catalog_product_id);
CREATE INDEX IF NOT EXISTS "IX_formula_cart_items_customer_formula_id" ON public.formula_cart_items USING btree (customer_formula_id);
CREATE INDEX IF NOT EXISTS "IX_customer_addresses_customer_id" ON public.customer_addresses USING btree (customer_id);
CREATE INDEX IF NOT EXISTS "IX_customer_devices_customer_id" ON public.customer_devices USING btree (customer_id);
CREATE INDEX IF NOT EXISTS "IX_customer_devices_device_token" ON public.customer_devices USING btree (device_token);
CREATE INDEX IF NOT EXISTS "IX_label_print_logs_generated_label_id" ON public.label_print_logs USING btree (generated_label_id);
CREATE INDEX IF NOT EXISTS "IX_label_print_logs_printed_by_id" ON public.label_print_logs USING btree (printed_by_id);
CREATE INDEX IF NOT EXISTS "IX_pending_two_factor_sessions_employee_id" ON public.pending_two_factor_sessions USING btree (employee_id);
CREATE INDEX IF NOT EXISTS "IX_pending_two_factor_sessions_expires_at" ON public.pending_two_factor_sessions USING btree (expires_at);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_pending_two_factor_sessions_temp_token" ON public.pending_two_factor_sessions USING btree (temp_token);
CREATE INDEX IF NOT EXISTS "IX_revoked_jwts_expires_at" ON public.revoked_jwts USING btree (expires_at);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_revoked_jwts_jwt_id" ON public.revoked_jwts USING btree (jwt_id);
CREATE INDEX IF NOT EXISTS "IX_search_history_created_at" ON public.search_history USING btree (created_at);
CREATE INDEX IF NOT EXISTS "IX_search_history_customer_id" ON public.search_history USING btree (customer_id);
CREATE INDEX IF NOT EXISTS "IX_search_history_search_term" ON public.search_history USING btree (search_term);
