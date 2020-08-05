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
            var dt = GetRecords(@"Select Calc.STOK_KODU,Calc.ADET,STOK_ADI as Açıklama from  dbo.TBLSTSABIT  INNER JOIN 
	                                (Select STOK_KODU,ISNULL((SELECT Sum(STHAR_GCMIK) as ADET
	                                FROM  dbo.TBLSTHAR 
	                                WHERE [STOK_KODU] = newTable.Stok_kodu AND STHAR_GCKOD ='G'  Group By STOK_KODU),0) - ISNULL((SELECT Sum(STHAR_GCMIK)
	                                FROM  dbo.TBLSTHAR
	                                WHERE [STOK_KODU] = newTable.Stok_kodu AND STHAR_GCKOD ='C' Group By STOK_KODU),0) as ADET FROM  
	                                (Select st.STOK_KODU, st.STOK_ADI,st.SATIS_FIAT1,st.ALIS_DOV_TIP,st.SAT_DOV_TIP,
	                                sth.STHAR_GCMIK,sth.STHAR_GCKOD,sth.STHAR_TARIH, sth.STHAR_HTUR, sth.STHAR_DOVTIP, sth.STHAR_DOVFIAT, sth.SUBE_KODU  
	                                from [dbo].[TBLSTSABIT] as st Left Join [dbo].TBLSTHAR as sth on st.STOK_KODU = sth.STOK_KODU) as newTable
	                                --Where STOK_KODU='EopyTest2'
	                                Group By STOK_KODU) as Calc
                                    ON dbo.TBLSTSABIT.STOK_KODU = Calc.STOK_KODU ORDER BY STOK_KODU;");

            if (dt != null)
                return dt.DefaultView;

            return new DataView();
        }

        public DataView GetStock(string stockCode)
        {
            var dt = GetRecords(string.Format (@"Select Calc.STOK_KODU,Calc.ADET,STOK_ADI as Açıklama from  dbo.TBLSTSABIT  INNER JOIN 
	                                            (Select STOK_KODU,ISNULL((SELECT Sum(STHAR_GCMIK) as ADET
	                                            FROM  dbo.TBLSTHAR 
	                                            WHERE [STOK_KODU] = newTable.Stok_kodu AND STHAR_GCKOD ='G'  Group By STOK_KODU),0) - ISNULL((SELECT Sum(STHAR_GCMIK)
	                                            FROM  dbo.TBLSTHAR
	                                            WHERE [STOK_KODU] = newTable.Stok_kodu AND STHAR_GCKOD ='C' Group By STOK_KODU),0) as ADET FROM  
	                                            (Select st.STOK_KODU, st.STOK_ADI,st.SATIS_FIAT1,st.ALIS_DOV_TIP,st.SAT_DOV_TIP,
	                                            sth.STHAR_GCMIK,sth.STHAR_GCKOD,sth.STHAR_TARIH, sth.STHAR_HTUR, sth.STHAR_DOVTIP, sth.STHAR_DOVFIAT, sth.SUBE_KODU  
	                                            from [dbo].[TBLSTSABIT] as st Left Join [dbo].TBLSTHAR as sth on st.STOK_KODU = sth.STOK_KODU) as newTable
	                                            Where STOK_KODU='{0}'
	                                            Group By STOK_KODU) as Calc
                                                ON dbo.TBLSTSABIT.STOK_KODU = Calc.STOK_KODU ORDER BY STOK_KODU;", stockCode));

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
                    IF EXISTS (SELECT 1 FROM dbo.TBLSTSABIT WHERE [STOK_KODU] =  @stockCode)
                    BEGIN
	                    SELECT @YeniMiktar = @newAmount;
	                    SELECT @EskiMiktar = ISNULL((Select ISNULL((SELECT Sum(STHAR_GCMIK)
			                    FROM  dbo.TBLSTHAR
			                    WHERE [STOK_KODU] = a.Stok_kodu AND STHAR_GCKOD ='G'  Group By STOK_KODU),0) - ISNULL((SELECT Sum(STHAR_GCMIK)
			                    FROM  dbo.TBLSTHAR
			                    WHERE [STOK_KODU] = a.Stok_kodu AND STHAR_GCKOD ='C' Group By STOK_KODU),0) as Adet from  dbo.TBLSTHAR as a 
			                    Where STOK_KODU= @stockCode 
			                    Group By STOK_KODU),0);

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
