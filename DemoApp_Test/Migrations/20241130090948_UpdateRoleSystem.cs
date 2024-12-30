using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoApp_Test.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoleSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brand",
                columns: table => new
                {
                    Brand_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FoundingDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brand", x => x.Brand_id);
                });

            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    Client_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.Client_id);
                });

            migrationBuilder.CreateTable(
                name: "Ice",
                columns: table => new
                {
                    Ice_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IceDetail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ice", x => x.Ice_id);
                });

            migrationBuilder.CreateTable(
                name: "Size",
                columns: table => new
                {
                    Size_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SizeDetail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Size", x => x.Size_id);
                });

            migrationBuilder.CreateTable(
                name: "Sugar",
                columns: table => new
                {
                    Sugar_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SugarDetail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sugar", x => x.Sugar_id);
                });

            migrationBuilder.CreateTable(
                name: "TypeCoffee",
                columns: table => new
                {
                    Type_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeCoffee", x => x.Type_id);
                });

            migrationBuilder.CreateTable(
                name: "Voucher",
                columns: table => new
                {
                    Voucher_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VoucherType = table.Column<bool>(type: "bit", nullable: false),
                    Detail = table.Column<double>(type: "float", nullable: false),
                    ExpirationDate = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voucher", x => x.Voucher_id);
                });

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Client_id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Username);
                    table.ForeignKey(
                        name: "FK_Account_Client_Client_id",
                        column: x => x.Client_id,
                        principalTable: "Client",
                        principalColumn: "Client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bill",
                columns: table => new
                {
                    Bill_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Total = table.Column<double>(type: "float", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    Client_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaymentStatus = table.Column<bool>(type: "bit", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bill", x => x.Bill_id);
                    table.ForeignKey(
                        name: "FK_Bill_Client_Client_id",
                        column: x => x.Client_id,
                        principalTable: "Client",
                        principalColumn: "Client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Product_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Discount = table.Column<int>(type: "int", nullable: false),
                    Brand_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type_id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Product_id);
                    table.ForeignKey(
                        name: "FK_Product_Brand_Brand_id",
                        column: x => x.Brand_id,
                        principalTable: "Brand",
                        principalColumn: "Brand_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Product_TypeCoffee_Type_id",
                        column: x => x.Type_id,
                        principalTable: "TypeCoffee",
                        principalColumn: "Type_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bill_Voucher",
                columns: table => new
                {
                    Voucher_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Bill_id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bill_Voucher", x => new { x.Voucher_id, x.Bill_id });
                    table.ForeignKey(
                        name: "FK_Bill_Voucher_Bill_Bill_id",
                        column: x => x.Bill_id,
                        principalTable: "Bill",
                        principalColumn: "Bill_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bill_Voucher_Voucher_Voucher_id",
                        column: x => x.Voucher_id,
                        principalTable: "Voucher",
                        principalColumn: "Voucher_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shipping",
                columns: table => new
                {
                    Shipping_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bill_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bill_id1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipping", x => x.Shipping_id);
                    table.ForeignKey(
                        name: "FK_Shipping_Bill_Bill_id1",
                        column: x => x.Bill_id1,
                        principalTable: "Bill",
                        principalColumn: "Bill_id");
                });

            migrationBuilder.CreateTable(
                name: "Product_Bill",
                columns: table => new
                {
                    Product_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Bill_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_Bill", x => new { x.Product_id, x.Bill_id });
                    table.ForeignKey(
                        name: "FK_Product_Bill_Bill_Bill_id",
                        column: x => x.Bill_id,
                        principalTable: "Bill",
                        principalColumn: "Bill_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Product_Bill_Product_Product_id",
                        column: x => x.Product_id,
                        principalTable: "Product",
                        principalColumn: "Product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product_Ice",
                columns: table => new
                {
                    Ice_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Product_id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_Ice", x => new { x.Ice_id, x.Product_id });
                    table.ForeignKey(
                        name: "FK_Product_Ice_Ice_Ice_id",
                        column: x => x.Ice_id,
                        principalTable: "Ice",
                        principalColumn: "Ice_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Product_Ice_Product_Product_id",
                        column: x => x.Product_id,
                        principalTable: "Product",
                        principalColumn: "Product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product_Size",
                columns: table => new
                {
                    Size_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Product_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_Size", x => new { x.Size_id, x.Product_id });
                    table.ForeignKey(
                        name: "FK_Product_Size_Product_Product_id",
                        column: x => x.Product_id,
                        principalTable: "Product",
                        principalColumn: "Product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Product_Size_Size_Size_id",
                        column: x => x.Size_id,
                        principalTable: "Size",
                        principalColumn: "Size_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product_Sugar",
                columns: table => new
                {
                    Sugar_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Product_id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_Sugar", x => new { x.Sugar_id, x.Product_id });
                    table.ForeignKey(
                        name: "FK_Product_Sugar_Product_Product_id",
                        column: x => x.Product_id,
                        principalTable: "Product",
                        principalColumn: "Product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Product_Sugar_Sugar_Sugar_id",
                        column: x => x.Sugar_id,
                        principalTable: "Sugar",
                        principalColumn: "Sugar_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_Client_id",
                table: "Account",
                column: "Client_id");

            migrationBuilder.CreateIndex(
                name: "IX_Bill_Client_id",
                table: "Bill",
                column: "Client_id");

            migrationBuilder.CreateIndex(
                name: "IX_Bill_Voucher_Bill_id",
                table: "Bill_Voucher",
                column: "Bill_id");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Brand_id",
                table: "Product",
                column: "Brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Type_id",
                table: "Product",
                column: "Type_id");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Bill_Bill_id",
                table: "Product_Bill",
                column: "Bill_id");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Ice_Product_id",
                table: "Product_Ice",
                column: "Product_id");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Size_Product_id",
                table: "Product_Size",
                column: "Product_id");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Sugar_Product_id",
                table: "Product_Sugar",
                column: "Product_id");

            migrationBuilder.CreateIndex(
                name: "IX_Shipping_Bill_id1",
                table: "Shipping",
                column: "Bill_id1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "Bill_Voucher");

            migrationBuilder.DropTable(
                name: "Product_Bill");

            migrationBuilder.DropTable(
                name: "Product_Ice");

            migrationBuilder.DropTable(
                name: "Product_Size");

            migrationBuilder.DropTable(
                name: "Product_Sugar");

            migrationBuilder.DropTable(
                name: "Shipping");

            migrationBuilder.DropTable(
                name: "Voucher");

            migrationBuilder.DropTable(
                name: "Ice");

            migrationBuilder.DropTable(
                name: "Size");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Sugar");

            migrationBuilder.DropTable(
                name: "Bill");

            migrationBuilder.DropTable(
                name: "Brand");

            migrationBuilder.DropTable(
                name: "TypeCoffee");

            migrationBuilder.DropTable(
                name: "Client");
        }
    }
}
