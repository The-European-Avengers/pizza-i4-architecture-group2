/*
  Warnings:

  - You are about to drop the column `unit_price` on the `Order_Pizza` table. All the data in the column will be lost.
  - You are about to drop the column `price` on the `Pizza` table. All the data in the column will be lost.

*/
-- AlterTable
ALTER TABLE "Order_Pizza" DROP COLUMN "unit_price";

-- AlterTable
ALTER TABLE "Pizza" DROP COLUMN "price",
ADD COLUMN     "unit_price" DOUBLE PRECISION NOT NULL DEFAULT 10;
