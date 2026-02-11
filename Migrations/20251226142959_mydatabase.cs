using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class mydatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "datetime('now')"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RecurrencePattern = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    WeekOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    SpecificDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SpecificStartTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    SpecificEndTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountabilityCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaseCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EscalationLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedToWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RelatedTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    RelatedTaskName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OccurrenceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsConfidential = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountabilityCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountabilityMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaseId = table.Column<int>(type: "INTEGER", nullable: false),
                    SenderWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    VisibleToLevels = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReplyToMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    AttachmentPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AttachmentName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsConfidential = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountabilityMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountabilityMessages_AccountabilityCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "AccountabilityCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountabilityMessages_AccountabilityMessages_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalTable: "AccountabilityMessages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AttendanceDate = table.Column<DateTime>(type: "date", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Men = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Women = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Teenagers = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Children = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RecordedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecordedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditTrails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ChangedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    OldValues = table.Column<string>(type: "TEXT", nullable: false),
                    NewValues = table.Column<string>(type: "TEXT", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BreakRequestHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BreakRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ActionByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionByRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakRequestHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BreakRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PendingAssignments = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AssignmentHandler = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RelieveOfficerId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: true),
                    ApprovedByWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    RequestStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResubmissionRequestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalActionDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaseActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaseId = table.Column<int>(type: "INTEGER", nullable: false),
                    PerformedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseActions_AccountabilityCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "AccountabilityCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DirectorateId = table.Column<int>(type: "INTEGER", nullable: false),
                    HeadWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssistantHeadWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Directorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HeadWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssistantHeadWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Directorates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExcuseRequestHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExcuseRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ActionByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionByRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "TEXT", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcuseRequestHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExcuseRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomServiceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CustomServiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CustomServiceTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NominatedBackupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResubmissionRequestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalActionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedByWorkerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcuseRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcuseRequests_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferingTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OfferingTypeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PaymentMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RecordedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecordedByName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ApprovedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ApprovedByName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AdminComments = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RecordedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferingRecords_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatorName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileUpdateRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProposedChanges = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ApprovedByWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApproverWorkerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileUpdateRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecordNominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NominatorWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NomineeWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecordType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordNominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordNominations_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RejectionNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileUpdateRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", nullable: false),
                    RejectedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    RejectedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectionNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RejectionNotifications_ProfileUpdateRequests_ProfileUpdateRequestId",
                        column: x => x.ProfileUpdateRequestId,
                        principalTable: "ProfileUpdateRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaderWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MiddleName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Sex = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsFirstLogin = table.Column<bool>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DirectorateId = table.Column<int>(type: "INTEGER", nullable: true),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CanAccessAdminPanel = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanApproveProfiles = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManageWorkers = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewReports = table.Column<bool>(type: "INTEGER", nullable: false),
                    PassportPhotoPath = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OrdinationStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OrdinationLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastOrdinationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Profession = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Organization = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    WorkerStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MaritalStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WeddingAnniversary = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateJoinedChurch = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PreviousChurch = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PreviousChurchRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PreviousChurchUnit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HasBelieverBaptism = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasWorkerInTraining = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSOD = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasBibleCollege = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: true),
                    BelieverBaptismCertificatePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    WorkerInTrainingCertificatePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SODCertificatePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BibleCollegeCertificatePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ProfileSubmitted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Stage1Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                    Stage2Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CompliancePercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                    table.UniqueConstraint("AK_Workers_WorkerId", x => x.WorkerId);
                    table.ForeignKey(
                        name: "FK_Workers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Workers_Directorates_DirectorateId",
                        column: x => x.DirectorateId,
                        principalTable: "Directorates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Workers_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Workers_Workers_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkerHierarchies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    HierarchyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectorateId = table.Column<int>(type: "INTEGER", nullable: true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    ServiceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReportsToWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerHierarchies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerHierarchies_Directorates_DirectorateId",
                        column: x => x.DirectorateId,
                        principalTable: "Directorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerHierarchies_Workers_ReportsToWorkerId",
                        column: x => x.ReportsToWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerHierarchies_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountabilityCases_AssignedToWorkerId",
                table: "AccountabilityCases",
                column: "AssignedToWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountabilityCases_CreatedByWorkerId",
                table: "AccountabilityCases",
                column: "CreatedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountabilityCases_WorkerId",
                table: "AccountabilityCases",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountabilityMessages_CaseId",
                table: "AccountabilityMessages",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountabilityMessages_ReplyToMessageId",
                table: "AccountabilityMessages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountabilityMessages_SenderWorkerId",
                table: "AccountabilityMessages",
                column: "SenderWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AttendanceDate",
                table: "AttendanceRecords",
                column: "AttendanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_RecordedByWorkerId",
                table: "AttendanceRecords",
                column: "RecordedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ServiceId",
                table: "AttendanceRecords",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ServiceName",
                table: "AttendanceRecords",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_ChangedAt",
                table: "AuditTrails",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_ChangedByWorkerId",
                table: "AuditTrails",
                column: "ChangedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_TableName_RecordId",
                table: "AuditTrails",
                columns: new[] { "TableName", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_BreakRequestHistories_ActionByWorkerId",
                table: "BreakRequestHistories",
                column: "ActionByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakRequestHistories_BreakRequestId",
                table: "BreakRequestHistories",
                column: "BreakRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakRequests_ApprovedByWorkerId",
                table: "BreakRequests",
                column: "ApprovedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakRequests_RelieveOfficerId",
                table: "BreakRequests",
                column: "RelieveOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakRequests_SupervisorId",
                table: "BreakRequests",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakRequests_WorkerId",
                table: "BreakRequests",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseActions_CaseId",
                table: "CaseActions",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseActions_PerformedByWorkerId",
                table: "CaseActions",
                column: "PerformedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_AssistantHeadWorkerId",
                table: "Departments",
                column: "AssistantHeadWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DirectorateId",
                table: "Departments",
                column: "DirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadWorkerId",
                table: "Departments",
                column: "HeadWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name_DirectorateId",
                table: "Departments",
                columns: new[] { "Name", "DirectorateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Directorates_AssistantHeadWorkerId",
                table: "Directorates",
                column: "AssistantHeadWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Directorates_Code",
                table: "Directorates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Directorates_HeadWorkerId",
                table: "Directorates",
                column: "HeadWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Directorates_Name",
                table: "Directorates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequestHistories_ActionByWorkerId",
                table: "ExcuseRequestHistories",
                column: "ActionByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequestHistories_ExcuseRequestId",
                table: "ExcuseRequestHistories",
                column: "ExcuseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_ApprovedByWorkerId",
                table: "ExcuseRequests",
                column: "ApprovedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_NominatedBackupId",
                table: "ExcuseRequests",
                column: "NominatedBackupId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_ServiceId",
                table: "ExcuseRequests",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_SupervisorId",
                table: "ExcuseRequests",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequests_WorkerId",
                table: "ExcuseRequests",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingRecords_ApprovedByWorkerId",
                table: "OfferingRecords",
                column: "ApprovedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingRecords_OfferingTypeId",
                table: "OfferingRecords",
                column: "OfferingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingRecords_RecordedByWorkerId",
                table: "OfferingRecords",
                column: "RecordedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingRecords_ServiceId",
                table: "OfferingRecords",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingTypes_CreatedBy",
                table: "OfferingTypes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileUpdateRequests_ApprovedByWorkerId",
                table: "ProfileUpdateRequests",
                column: "ApprovedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileUpdateRequests_ApproverWorkerId",
                table: "ProfileUpdateRequests",
                column: "ApproverWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileUpdateRequests_WorkerId",
                table: "ProfileUpdateRequests",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordNominations_NominatorWorkerId",
                table: "RecordNominations",
                column: "NominatorWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordNominations_NomineeWorkerId",
                table: "RecordNominations",
                column: "NomineeWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordNominations_ServiceId_ServiceDate_NomineeWorkerId_RecordType",
                table: "RecordNominations",
                columns: new[] { "ServiceId", "ServiceDate", "NomineeWorkerId", "RecordType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RejectionNotifications_ProfileUpdateRequestId",
                table: "RejectionNotifications",
                column: "ProfileUpdateRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RejectionNotifications_RejectedByWorkerId",
                table: "RejectionNotifications",
                column: "RejectedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_RejectionNotifications_WorkerId",
                table: "RejectionNotifications",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name",
                table: "Services",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_DepartmentId",
                table: "Units",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_LeaderWorkerId",
                table: "Units",
                column: "LeaderWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name_DepartmentId",
                table: "Units",
                columns: new[] { "Name", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerHierarchies_DirectorateId",
                table: "WorkerHierarchies",
                column: "DirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerHierarchies_ReportsToWorkerId",
                table: "WorkerHierarchies",
                column: "ReportsToWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerHierarchies_WorkerId",
                table: "WorkerHierarchies",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_DepartmentId",
                table: "Workers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_DirectorateId",
                table: "Workers",
                column: "DirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_SupervisorId",
                table: "Workers",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_UnitId",
                table: "Workers",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_WorkerId",
                table: "Workers",
                column: "WorkerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountabilityCases_Workers_AssignedToWorkerId",
                table: "AccountabilityCases",
                column: "AssignedToWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountabilityCases_Workers_CreatedByWorkerId",
                table: "AccountabilityCases",
                column: "CreatedByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountabilityCases_Workers_WorkerId",
                table: "AccountabilityCases",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountabilityMessages_Workers_SenderWorkerId",
                table: "AccountabilityMessages",
                column: "SenderWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Workers_RecordedByWorkerId",
                table: "AttendanceRecords",
                column: "RecordedByWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditTrails_Workers_ChangedByWorkerId",
                table: "AuditTrails",
                column: "ChangedByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BreakRequestHistories_BreakRequests_BreakRequestId",
                table: "BreakRequestHistories",
                column: "BreakRequestId",
                principalTable: "BreakRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BreakRequestHistories_Workers_ActionByWorkerId",
                table: "BreakRequestHistories",
                column: "ActionByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BreakRequests_Workers_ApprovedByWorkerId",
                table: "BreakRequests",
                column: "ApprovedByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BreakRequests_Workers_RelieveOfficerId",
                table: "BreakRequests",
                column: "RelieveOfficerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BreakRequests_Workers_SupervisorId",
                table: "BreakRequests",
                column: "SupervisorId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BreakRequests_Workers_WorkerId",
                table: "BreakRequests",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseActions_Workers_PerformedByWorkerId",
                table: "CaseActions",
                column: "PerformedByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Directorates_DirectorateId",
                table: "Departments",
                column: "DirectorateId",
                principalTable: "Directorates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Workers_AssistantHeadWorkerId",
                table: "Departments",
                column: "AssistantHeadWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Workers_HeadWorkerId",
                table: "Departments",
                column: "HeadWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Directorates_Workers_AssistantHeadWorkerId",
                table: "Directorates",
                column: "AssistantHeadWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Directorates_Workers_HeadWorkerId",
                table: "Directorates",
                column: "HeadWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcuseRequestHistories_ExcuseRequests_ExcuseRequestId",
                table: "ExcuseRequestHistories",
                column: "ExcuseRequestId",
                principalTable: "ExcuseRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcuseRequestHistories_Workers_ActionByWorkerId",
                table: "ExcuseRequestHistories",
                column: "ActionByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcuseRequests_Workers_ApprovedByWorkerId",
                table: "ExcuseRequests",
                column: "ApprovedByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcuseRequests_Workers_NominatedBackupId",
                table: "ExcuseRequests",
                column: "NominatedBackupId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcuseRequests_Workers_SupervisorId",
                table: "ExcuseRequests",
                column: "SupervisorId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcuseRequests_Workers_WorkerId",
                table: "ExcuseRequests",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferingRecords_OfferingTypes_OfferingTypeId",
                table: "OfferingRecords",
                column: "OfferingTypeId",
                principalTable: "OfferingTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferingRecords_Workers_ApprovedByWorkerId",
                table: "OfferingRecords",
                column: "ApprovedByWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferingRecords_Workers_RecordedByWorkerId",
                table: "OfferingRecords",
                column: "RecordedByWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferingTypes_Workers_CreatedBy",
                table: "OfferingTypes",
                column: "CreatedBy",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileUpdateRequests_Workers_ApprovedByWorkerId",
                table: "ProfileUpdateRequests",
                column: "ApprovedByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileUpdateRequests_Workers_ApproverWorkerId",
                table: "ProfileUpdateRequests",
                column: "ApproverWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileUpdateRequests_Workers_WorkerId",
                table: "ProfileUpdateRequests",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecordNominations_Workers_NominatorWorkerId",
                table: "RecordNominations",
                column: "NominatorWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecordNominations_Workers_NomineeWorkerId",
                table: "RecordNominations",
                column: "NomineeWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RejectionNotifications_Workers_RejectedByWorkerId",
                table: "RejectionNotifications",
                column: "RejectedByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RejectionNotifications_Workers_WorkerId",
                table: "RejectionNotifications",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Workers_LeaderWorkerId",
                table: "Units",
                column: "LeaderWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Workers_AssistantHeadWorkerId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Workers_HeadWorkerId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Directorates_Workers_AssistantHeadWorkerId",
                table: "Directorates");

            migrationBuilder.DropForeignKey(
                name: "FK_Directorates_Workers_HeadWorkerId",
                table: "Directorates");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Workers_LeaderWorkerId",
                table: "Units");

            migrationBuilder.DropTable(
                name: "AccountabilityMessages");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AuditTrails");

            migrationBuilder.DropTable(
                name: "BreakRequestHistories");

            migrationBuilder.DropTable(
                name: "CaseActions");

            migrationBuilder.DropTable(
                name: "ExcuseRequestHistories");

            migrationBuilder.DropTable(
                name: "OfferingRecords");

            migrationBuilder.DropTable(
                name: "RecordNominations");

            migrationBuilder.DropTable(
                name: "RejectionNotifications");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "WorkerHierarchies");

            migrationBuilder.DropTable(
                name: "BreakRequests");

            migrationBuilder.DropTable(
                name: "AccountabilityCases");

            migrationBuilder.DropTable(
                name: "ExcuseRequests");

            migrationBuilder.DropTable(
                name: "OfferingTypes");

            migrationBuilder.DropTable(
                name: "ProfileUpdateRequests");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Workers");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Directorates");
        }
    }
}
