-- AlterTable
ALTER TABLE "Order" ADD COLUMN     "total_price" DECIMAL(65,30) NOT NULL DEFAULT 0;

-- AlterTable
ALTER TABLE "Order_Pizza" ADD COLUMN     "unit_price" DECIMAL(65,30) NOT NULL DEFAULT 0;
