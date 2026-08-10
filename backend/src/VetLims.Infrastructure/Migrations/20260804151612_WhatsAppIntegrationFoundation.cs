using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WhatsAppIntegrationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "whatsapp_templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HeaderText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BodyTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    FooterText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_message_queue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ToNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: true),
                    OwnerId = table.Column<int>(type: "integer", nullable: true),
                    RegistrationId = table.Column<int>(type: "integer", nullable: true),
                    RegistrationItemId = table.Column<int>(type: "integer", nullable: true),
                    AppointmentId = table.Column<int>(type: "integer", nullable: true),
                    VaccinationRecordId = table.Column<int>(type: "integer", nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MessageHeader = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MessageBody = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    MessageFooter = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_message_queue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_whatsapp_message_queue_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_whatsapp_message_queue_owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_whatsapp_message_queue_registration_items_RegistrationItemId",
                        column: x => x.RegistrationItemId,
                        principalTable: "registration_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_whatsapp_message_queue_registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_whatsapp_message_queue_vaccination_records_VaccinationRecor~",
                        column: x => x.VaccinationRecordId,
                        principalTable: "vaccination_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_whatsapp_message_queue_whatsapp_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "whatsapp_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_AppointmentId",
                table: "whatsapp_message_queue",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_MessageType_CreatedDate",
                table: "whatsapp_message_queue",
                columns: new[] { "MessageType", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_OwnerId",
                table: "whatsapp_message_queue",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_ProviderMessageId",
                table: "whatsapp_message_queue",
                column: "ProviderMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_RegistrationId",
                table: "whatsapp_message_queue",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_RegistrationItemId",
                table: "whatsapp_message_queue",
                column: "RegistrationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_Status_NextAttemptAt",
                table: "whatsapp_message_queue",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_TemplateId",
                table: "whatsapp_message_queue",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_queue_VaccinationRecordId",
                table: "whatsapp_message_queue",
                column: "VaccinationRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_templates_Code",
                table: "whatsapp_templates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_templates_MessageType_LanguageCode_IsActive",
                table: "whatsapp_templates",
                columns: new[] { "MessageType", "LanguageCode", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsapp_message_queue");

            migrationBuilder.DropTable(
                name: "whatsapp_templates");
        }
    }
}
