using SNNetsisStokTakip.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNNetsisStokTakip.Classes
{
    public class SQLOperations
    {

        public string GenerateConnStr(ModelConnStr modelConnStr)
        {
            string connStr = "";
            if (modelConnStr != null)
            {
                connStr = string.Format("Data Source={0}", modelConnStr.Server);

                if (!string.IsNullOrEmpty(modelConnStr.DbName))
                    connStr = connStr + string.Format(";Initial Catalog={0}", modelConnStr.DbName);

                if (!string.IsNullOrEmpty(modelConnStr.User))
                    connStr = connStr + string.Format(";User ID={0};Password={1}", modelConnStr.User, modelConnStr.Pass);
            }
            return connStr;
        }

        public List<string> GetDBNames(ModelConnStr modelConnStr)
        {
            var result = new List<string>();
            using (var conn = new SqlConnection(GenerateConnStr(modelConnStr)))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT name from sys.databases", conn))
                {
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            result.Add((string)dr[0]);
                        }
                    }
                    conn.Close();
                }
            }


            return result;
        }

        public DataView GetAllStocks()
        {
            var dt = GetRecords(@"Select STOK_KODU,ISNULL((SELECT Sum(STHAR_GCMIK)
                                FROM  dbo.TBLSTHAR
                                WHERE[STOK_KODU] = a.STOK_KODU AND STHAR_GCKOD = 'G'  Group By STOK_KODU),0) -ISNULL((SELECT Sum(STHAR_GCMIK)
                                FROM  dbo.TBLSTHAR
                                WHERE[STOK_KODU] = a.STOK_KODU AND STHAR_GCKOD = 'C' Group By STOK_KODU),0) as Adet from dbo.TBLSTHAR as a
                                Group By STOK_KODU");

            if (dt != null)
                return dt.DefaultView;

            return new DataView();
        }

        public DataView GetStock(string stockCode)
        {
            var dt = GetRecords(string.Format (@"Select STOK_KODU,ISNULL((SELECT Sum(STHAR_GCMIK)
                                FROM  dbo.TBLSTHAR
                                WHERE[STOK_KODU] = a.STOK_KODU AND STHAR_GCKOD = 'G'  Group By STOK_KODU),0) -ISNULL((SELECT Sum(STHAR_GCMIK)
                                FROM  dbo.TBLSTHAR
                                WHERE[STOK_KODU] = a.STOK_KODU AND STHAR_GCKOD = 'C' Group By STOK_KODU),0) as Adet from dbo.TBLSTHAR as a
                                WHERE STOK_KODU = '{0}'
                                Group By STOK_KODU ", stockCode));

            if (dt != null)
                return dt.DefaultView;

            return new DataView();
        }


        public DataTable GetRecords(string query)
        {
            using (var conn = new SqlConnection(ConnectionManagement.SqlOperations.GenerateConnStr(ConnectionManagement.ConnStr)))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable("dbo.TBLSTHAR");
                    sda.Fill(dt);

                    return dt;
                }
            }
        }

        public bool IfExistStockCode(string stockCode)
        {
            using (var conn = new SqlConnection(ConnectionManagement.SqlOperations.GenerateConnStr(ConnectionManagement.ConnStr)))
            {
                using (var cmd = new SqlCommand("SELECT COUNT(*) from dbo.TBLSTHAR where STOK_KODU = @stockCode", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@stockCode", stockCode);
                    int recordCount = (int)cmd.ExecuteScalar();
                    if (recordCount > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public int InsertNewStockAmount(string stockCode, int amount)
        {
            string query =
                    @"--Buradan Yeni Miktar Eklemesi Yapılıyor  
                    -- (-1) stok yok , (0) kayıt eklenmedi, (1) kayıt eklendi
                    DECLARE @EskiMiktar int;
                    DECLARE @YeniMiktar int;
                    DECLARE @Result int;
                    SELECT @Result = -1;
                    IF EXISTS (SELECT 1 FROM dbo.TBLSTHAR WHERE [STOK_KODU] =  @stockCode)
                    BEGIN
	                    SELECT @YeniMiktar = @newAmount;
	                    SELECT @EskiMiktar = (Select ISNULL((SELECT Sum(STHAR_GCMIK)
			                    FROM  dbo.TBLSTHAR
			                    WHERE [STOK_KODU] = a.Stok_kodu AND STHAR_GCKOD ='G'  Group By STOK_KODU),0) - ISNULL((SELECT Sum(STHAR_GCMIK)
			                    FROM  dbo.TBLSTHAR
			                    WHERE [STOK_KODU] = a.Stok_kodu AND STHAR_GCKOD ='C' Group By STOK_KODU),0) as Adet from  dbo.TBLSTHAR as a 
			                    Where STOK_KODU= @stockCode 
			                    Group By STOK_KODU);

	                    IF @EskiMiktar <> @YeniMiktar 
	                    BEGIN
		                    INSERT INTO TBLSTHAR (STOK_KODU, STHAR_GCMIK, STHAR_GCKOD, STHAR_TARIH, STHAR_HTUR, STHAR_DOVTIP, STHAR_DOVFIAT, SUBE_KODU)
		                    VALUES ( @stockCode, ABS(@YeniMiktar - @EskiMiktar),
		                    CASE 
			                    WHEN @EskiMiktar < @YeniMiktar THEN 'G'
			                    WHEN @EskiMiktar  > @YeniMiktar THEN 'C' 
		                    END
		                    ,@DateTime,'A' ,0 ,0.000000000000000,0);
	                    END
	                    SELECT @Result =  @@ROWCOUNT;
                    END

                    SELECT @Result";

            using (var conn = new SqlConnection(ConnectionManagement.SqlOperations.GenerateConnStr(ConnectionManagement.ConnStr)))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@newAmount", amount);
                    cmd.Parameters.AddWithValue("@DateTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@stockCode", stockCode);
                    int recordCount = (int)cmd.ExecuteScalar();

                    return recordCount;
                }
            }
        }
    }
}
