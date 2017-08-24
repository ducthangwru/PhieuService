using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DongBoPhieuService
{
    partial class DBPhieuService : ServiceBase
    {
        private static Log_Sytems logs = new Log_Sytems();

        public int soPhieuIH = 0;
        public int soPhieuBI = 0;
        public int soPhieuVOC = 0;

        public int soPhieuIHFail = 0;
        public int soPhieuBIFail = 0;
        public int soPhieuVOCFail = 0;

        public int idDongBoIH = 0;
        public int idDongBoBI = 0;
        public int idDongBoVOC = 0;

        public string timeDongBo = "";

        private int typeRun = 0;
        private static string strConnect1 = ConfigurationManager.AppSettings["ConnectASC"].ToString();
        private static string strConnect2 = ConfigurationManager.AppSettings["Connect31"].ToString();
        private static string strConnect3 = ConfigurationManager.AppSettings["Connect"].ToString();
        bool stopping_GetData = false;
        ASCService.asc_serviceSoapClient client2 = new ASCService.asc_serviceSoapClient();
        private static DataAccess db3 = new DataAccess(ConfigurationManager.AppSettings["Connect"].ToString());

        ManualResetEvent stoppedEvent;

        public DBPhieuService()
        {
            InitializeComponent();

            //1f= 60s = 100.000
            //timmer = new Timer(60000 * value_time);
            //timmer.Elapsed += new ElapsedEventHandler(timmer_Elapsed);
            this.stopping_GetData = false;

            this.stoppedEvent = new ManualResetEvent(false);

        }

        private void ThreadGetData(object state)
        {
            // Periodically check if the service is stopping.
            logs.ErrorLog("DongBoPhieuService GetData Begin loop", "");

            string err = "";


            while (!this.stopping_GetData)
            {
                // Perform main service function here...

                timeDongBo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                try
                {
                    
                    DataTable dt01 = db3.ExecuteQueryDataSet("select * from PhieuSuaChua_Current", CommandType.Text, null);
                    DataTable dt02 = db3.ExecuteQueryDataSet("select * from PhieuVOC_Phieu_Current", CommandType.Text, null);
                    DataTable dt03 = db3.ExecuteQueryDataSet("select * from PhieuTuVanBaoDuong_Current", CommandType.Text, null);

                    if (dt01.Rows.Count > 0)
                    {
                        soPhieuIH = 0;
                        soPhieuIHFail = 0;
                        List<SqlParameter> param = new List<SqlParameter>();
                        param.Add(new SqlParameter("@tongsophieu", dt01.Rows.Count));

                        idDongBoIH = int.Parse(db3.MyExecuteScalar("usp_Service_ThemBaoCaoPhieuSuaChua", CommandType.StoredProcedure, ref err, param).ToString());

                        foreach (DataRow dr in dt01.Rows)
                        {
                            UpdatePhieuBH(dr);
                            // UpdatePhieuVOCIH(dr);
                        }

                        List<SqlParameter> param1 = new List<SqlParameter>();
                        param1.Add( new SqlParameter("@iddongbo", idDongBoIH));
                        param1.Add(new SqlParameter("@sophieuthanhcong", soPhieuIH));
                        param1.Add(new SqlParameter("@sophieuthatbai", soPhieuIHFail));

                        db3.MyExecuteNonQuery("usp_Service_UpdateBaoCaoPhieuSuaChua", CommandType.StoredProcedure, ref err, param1);
                    }


                    
                    if (dt02.Rows.Count > 0)
                    {
                        soPhieuVOC = 0;
                        soPhieuVOCFail = 0;
                        List<SqlParameter> param = new List<SqlParameter>();
                        param.Add(new SqlParameter("@tongsophieu", dt02.Rows.Count));

                        idDongBoVOC = int.Parse(db3.MyExecuteScalar("usp_Service_ThemBaoCaoPhieuVOC", CommandType.StoredProcedure, ref err, param).ToString());


                        foreach (DataRow dr in dt02.Rows)
                        {
                            //if (!UpdatePhieuBH(dr))
                            //    MessageBox.Show("false");
                            UpdatePhieuVOC(dr);
                        }

                        List<SqlParameter> param1 = new List<SqlParameter>();
                        param1.Add(new SqlParameter("@iddongbo", idDongBoVOC));
                        param1.Add(new SqlParameter("@sophieuthanhcong", soPhieuVOC));
                        param1.Add(new SqlParameter("@sophieuthatbai", soPhieuVOCFail));

                        db3.MyExecuteNonQuery("usp_Service_UpdateBaoCaoPhieuVOC", CommandType.StoredProcedure, ref err, param1);
                    }



                    if (dt03.Rows.Count > 0)
                    {
                        soPhieuBI = 0;
                        soPhieuBIFail = 0;
                        List<SqlParameter> param = new List<SqlParameter>();
                        param.Add(new SqlParameter("@tongsophieu", dt03.Rows.Count));

                        idDongBoBI = int.Parse(db3.MyExecuteScalar("usp_Service_ThemBaoCaoPhieuTVBaoDuong", CommandType.StoredProcedure, ref err, param).ToString());

                        foreach (DataRow dr in dt03.Rows)
                        {
                            UpdatePhieuBI(dr);
                        }

                        List<SqlParameter> param1 = new List<SqlParameter>();
                        param1.Add(new SqlParameter("@iddongbo", idDongBoBI));
                        param1.Add(new SqlParameter("@sophieuthanhcong", soPhieuBI));
                        param1.Add(new SqlParameter("@sophieuthatbai", soPhieuBIFail));

                        db3.MyExecuteNonQuery("usp_Service_UpdateBaoCaoPhieuTVBaoDuong", CommandType.StoredProcedure, ref err, param1);
                    }

                }
                catch (Exception ex)
                {
                    logs.ErrorLog(ex.Message, ex.StackTrace);
                }



                // string thoigian = ConfigurationManager.AppSettings["sophut"].ToString();
                //int value_time = 0;

                logs.ErrorLog("Dong bo luc :" + DateTime.Now.AddMinutes(double.Parse(ConfigurationManager.AppSettings["TANSUAT"])), "");
                Thread.Sleep((int)(60000 * double.Parse(ConfigurationManager.AppSettings["TANSUAT"])));  // Simulate some lengthy operations.
            }
            logs.ErrorLog("Dong bo End loop", "");

            // Signal the stopped event.
            this.stoppedEvent.Set();
        }

        public bool UpdatePhieuBH(DataRow dr)
        {
            string err = "";
            string def = "";
            int kieunhap = 1;
            int result = 0;
            int kieuphieu = 0;
            string ten_dn = "asc.portal";
            string mat_khau = "abc@123";
            string sophieu = dr["SoPhieu"].ToString();
            string makho = dr["MaKho"].ToString();
            string ngaytiepnhan = DateTime.Parse(dr["NgayTiepNhan"].ToString()).ToString("dd/MM/yyyy");
            string giotiepnhan = DateTime.Parse(dr["NgayTiepNhan"].ToString()).ToString("HH:mm:ss");
            string ngayhentra = DateTime.Parse(dr["NgayHenTra"].ToString()).ToString("dd/MM/yyyy");
            string giohentra = DateTime.Parse(dr["NgayHenTra"].ToString()).ToString("HH:mm:ss");
            int loaitiepnhan = int.Parse(dr["LoaiTiepNhan"].ToString());
            string nguoitn = dr["NguoiTiepNhan"].ToString();
            string tenkh = dr["TenKH"].ToString();
            string sdt = dr["SDT"].ToString();
            string diachi = dr["DiaChi"].ToString();
            string tinh = dr["Tinh"].ToString();
            string huyen = dr["Huyen"].ToString();
            string xa = dr["Xa"].ToString();
            string maht = dr["MaHienTuong"].ToString();
            string motaloi = dr["MoTaLoi"].ToString();
            string serial = dr["SerialSP"].ToString();
            string ghichu = dr["GhiChu"].ToString();
            int trangthai = int.Parse(dr["TrangThaiPhieu"].ToString());
            string goibaoduong = (dr["GoiBaoDuong"] != null) ? dr["GoiBaoDuong"].ToString() : "";
            string sophieuBI = (dr["SoPhieuBI"] != null) ? dr["SoPhieuBI"].ToString() : "";
            string manhomKH = (dr["MaNhomKH"] != null) ? dr["MaNhomKH"].ToString() : "";
            string tel1  = (dr["SDTLienLac"] != null) ? dr["SDTLienLac"].ToString() : "";
            string tel2 = (dr["SDTKTVSua"] != null) ? dr["SDTKTVSua"].ToString() : "";
            string NhomSP = (dr["NhomSP"] != null) ? dr["NhomSP"].ToString() : "";
            string ModelSP = (dr["ModelSP"] != null) ? dr["ModelSP"].ToString() : "";

            try
            {

                //  result = client2.UpdatePhieu("asc.portal", "abc@123", 1, "IH117081500046", "",
                //"15/08/2017", "10:22:32", 2, "15/08/2017", "10:22:32",
                //"1107", "Nguyễn thị trúc", "100", "", "Hoàng Xá", "717", "0710", "071001", "BLCT01", "Mô tả lỗi", "0000047", 0, "", "Ghi chú", 98);

                result = client2.UpdatePhieu(ten_dn, mat_khau, kieunhap, sophieu, sophieuBI, makho,
                ngaytiepnhan, giotiepnhan, loaitiepnhan, ngayhentra, giohentra,
                nguoitn, tenkh, manhomKH, sdt, tel1, tel2, diachi, tinh, huyen, xa, maht, motaloi, serial, NhomSP, ModelSP, kieuphieu, goibaoduong, ghichu, trangthai);
            }
            catch (Exception ex)
            {
                soPhieuIHFail++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoIH));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieu"].ToString()));
                param1.Add(new SqlParameter("@trangthai", false));
                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuSuaChua", CommandType.StoredProcedure, ref err, param1);

                logs.ErrorLog(ex.StackTrace, ex.Message);
                return false;
            }


            if (result == 200 || result == 201)
            {
                soPhieuIH++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoIH));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieu"].ToString()));
                param1.Add(new SqlParameter("@trangthai", true));
                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuSuaChua", CommandType.StoredProcedure, ref err, param1);

                try
                {
                    List<SqlParameter> param = new List<SqlParameter>();

                    param.Add(new SqlParameter("@sophieu", dr["SoPhieu"].ToString()));
                    param.Add(new SqlParameter("@makho", dr["MaKho"].ToString()));
                    param.Add(new SqlParameter("@ngaytiepnhan", dr["NgayTiepNhan"].ToString()));
                    param.Add(new SqlParameter("@ngayhentra", dr["NgayHenTra"].ToString()));
                    param.Add(new SqlParameter("@ngaydukien", dr["NgayDuKien"].ToString()));
                    param.Add(new SqlParameter("@nguoitn", dr["NguoiTiepNhan"].ToString()));
                    param.Add(new SqlParameter("@donvi", dr["DonVi"].ToString()));
                    param.Add(new SqlParameter("@loaitiepnhan", dr["LoaiTiepNhan"].ToString()));
                    param.Add(new SqlParameter("@sdt", dr["SDT"].ToString()));
                    param.Add(new SqlParameter("@tenkh", dr["TenKH"].ToString()));
                    param.Add(new SqlParameter("@diachi", dr["DiaChi"].ToString()));
                    param.Add(new SqlParameter("@tinh", dr["Tinh"].ToString()));
                    param.Add(new SqlParameter("@huyen", dr["Huyen"].ToString()));
                    param.Add(new SqlParameter("@xa", dr["Xa"].ToString()));
                    param.Add(new SqlParameter("@NhomSP", dr["NhomSP"].ToString()));
                    param.Add(new SqlParameter("@ModelSP", dr["ModelSP"].ToString()));
                    param.Add(new SqlParameter("@SerialSP", dr["SerialSP"].ToString()));
                    param.Add(new SqlParameter("@MaHT", dr["MaHienTuong"].ToString()));
                    param.Add(new SqlParameter("@MoTaLoi", dr["MoTaLoi"].ToString()));
                    param.Add(new SqlParameter("@PhanCongKTV", dr["PhanCongKTV"].ToString()));
                    param.Add(new SqlParameter("@ghichu", dr["GhiChu"].ToString()));
                    param.Add(new SqlParameter("@TrangThai", int.Parse(dr["TrangThaiPhieu"].ToString())));
                    param.Add(new SqlParameter("@GoiBaoDuong", dr["GoiBaoDuong"].ToString()));
                    param.Add(new SqlParameter("@SoPhieuBI", dr["SoPhieuBI"].ToString()));
                    param.Add(new SqlParameter("@SDTLienLac", dr["SDTLienLac"] != null ? dr["SDTLienLac"].ToString() : ""));
                    param.Add(new SqlParameter("@SDTKTVSua", dr["SDTKTVSua"] != null ? dr["SDTKTVSua"].ToString() : ""));
                    param.Add(new SqlParameter("@MaNhomKH", dr["MaNhomKH"] != null ? dr["MaNhomKH"].ToString() : ""));
                    param.Add(new SqlParameter("@NoiDung", dr["NoiDung"] != null ? dr["NoiDung"].ToString() : ""));

                    if (db3.MyExecuteNonQuery("usp_Service_ThemPhieuBaoHanh", CommandType.StoredProcedure, ref err, param))
                    {
                        db3.MyExecuteNonQuery("delete PhieuSuaChua_Current where SoPhieu like '" + dr["SoPhieu"].ToString() + "'", CommandType.Text, ref err, null);
                    }
                }
                catch (Exception ex)
                {
                    logs.ErrorLog(ex.StackTrace, ex.Message);
                    return false;
                }
            }
            else
            {
                logs.ErrorLog("Loi Service ", "Code = " + result);

                soPhieuIHFail++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoIH));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieu"].ToString()));
                param1.Add(new SqlParameter("@trangthai", false));

                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuSuaChua", CommandType.StoredProcedure, ref err, param1);

                return false;
            }

            return true;
        }

        public void UpdatePhieuVOC(DataRow dr)
        {
            int result = 0;
            int phieuchinh = 0;
            string err = "";
            string username = "";
            try
            {
                username = db3.MyExecuteScalar("select PhanCongKTV from PhieuSuaChua_ASC where SoPhieu like '" + dr["SoPhieu"].ToString() + "'", CommandType.Text, ref err, null).ToString();
            }
            catch (Exception ex)
            {
               
            }

            try
            {
                phieuchinh = (bool.Parse(dr["PhieuChinh"].ToString()) == true) ? 1 : 0;
                result = client2.UpdateVOC("asc.portal", "abc@123", 1, dr["SoPhieuVOC"].ToString(), dr["SoPhieu"].ToString(),
                username, DateTime.Parse(dr["NgayGio"].ToString()).ToString("dd/MM/yyyy"),
                DateTime.Parse(dr["NgayGio"].ToString()).ToString("HH:mm:ss"), dr["KHPhanAnh"].ToString(),
                phieuchinh, int.Parse(dr["MucDo"].ToString()), int.Parse(dr["TrangThai"].ToString()));
            }
            catch (Exception ex)
            {
                soPhieuVOCFail++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoVOC));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieuVOC"].ToString()));
                param1.Add(new SqlParameter("@trangthai", false));
                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuVOC", CommandType.StoredProcedure, ref err, param1);

                return;
            }

            if (result == 200 || result == 201)
            {
                soPhieuVOC++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoVOC));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieuVOC"].ToString()));
                param1.Add(new SqlParameter("@trangthai", true));
                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuVOC", CommandType.StoredProcedure, ref err, param1);

                try
                {
                    List<SqlParameter> param = new List<SqlParameter>();

                    param.Add(new SqlParameter("@SoPhieu", dr["SoPhieuVOC"].ToString()));
                    param.Add(new SqlParameter("@SoPhieuIH", dr["SoPhieu"].ToString()));
                    param.Add(new SqlParameter("@KHPhanAnh", dr["KHPhanAnh"].ToString()));
                    param.Add(new SqlParameter("@GhiChu", dr["GhiChu"].ToString()));
                    param.Add(new SqlParameter("@TrangThai", dr["TrangThai"].ToString()));
                    param.Add(new SqlParameter("@PhieuChinh", bool.Parse(dr["PhieuChinh"].ToString())));
                    param.Add(new SqlParameter("@MucDo", int.Parse(dr["MucDo"].ToString())));
                    param.Add(new SqlParameter("@NgayGio", dr["NgayGio"].ToString()));
                    param.Add(new SqlParameter("@NguoiGiaiTrinh", dr["NguoiGiaiTrinh"].ToString()));

                    if (db3.MyExecuteNonQuery("usp_Service_InsertOrUpdatePhieuVOC", CommandType.StoredProcedure, ref err, param))
                        db3.MyExecuteNonQuery("delete PhieuVOC_Phieu_Current where SoPhieuVOC like '" + dr["SoPhieuVOC"].ToString() + "'", CommandType.Text, ref err, null);
                }
                catch(Exception ex)
                {
                    logs.ErrorLog(ex.StackTrace, ex.Message);
                }
            }
            else
            {
                logs.ErrorLog("Loi Service ", "Code = " + result);

                soPhieuVOCFail++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoVOC));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieuVOC"].ToString()));
                param1.Add(new SqlParameter("@trangthai", false));
                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuVOC", CommandType.StoredProcedure, ref err, param1);
            }
        }

        public void UpdatePhieuBI(DataRow dr)
        {
            int result = 0;
            string err = "";
            try
            {
                result = client2.UpdatePhieu("asc.portal", "abc@123", 1, dr["SoPhieu"].ToString(),"", "",
                    DateTime.Parse(dr["NgayTiepNhan"].ToString()).ToString("dd/MM/yyyy"),
                    DateTime.Parse(dr["NgayTiepNhan"].ToString()).ToString("HH:mm:ss"),
                     0, "", "", dr["NguoiTiepNhan"].ToString(), dr["TenKH"].ToString(), "", dr["SDT"].ToString(), dr["SDTLienLac"].ToString(), dr["SDTKTVSua"].ToString(),
                     dr["DiaChi"].ToString(), dr["Tinh"].ToString(), dr["Huyen"].ToString(), dr["Xa"].ToString(),"", dr["KHPhanAnh"].ToString(), "", "", "", 2,
                     dr["GoiTuVan"].ToString(), dr["GhiChu"].ToString(), int.Parse(dr["TrangThai"].ToString()));
            }
            catch (Exception ex)
            {
                soPhieuBIFail++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoBI));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieu"].ToString()));
                param1.Add(new SqlParameter("@trangthai", false));
                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuTVBaoDuong", CommandType.StoredProcedure, ref err, param1);

                logs.ErrorLog(ex.StackTrace, ex.Message);
                return;
            }

            if (result == 200 || result == 201)
            {
                soPhieuBI++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoBI));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieu"].ToString()));
                param1.Add(new SqlParameter("@trangthai", true));
                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuTVBaoDuong", CommandType.StoredProcedure, ref err, param1);

                try
                {
                    List<SqlParameter> param = new List<SqlParameter>();
                    param.Add(new SqlParameter("@SoPhieu", dr["SoPhieu"].ToString()));
                    param.Add(new SqlParameter("@NgayTiepNhan", dr["NgayTiepNhan"].ToString()));
                    param.Add(new SqlParameter("@NguoiTiepNhan", dr["NguoiTiepNhan"].ToString()));
                    param.Add(new SqlParameter("@DonVi", dr["DonVi"].ToString()));
                    param.Add(new SqlParameter("@SDT", dr["SDT"].ToString()));
                    param.Add(new SqlParameter("@TenKH", dr["TenKH"].ToString()));
                    param.Add(new SqlParameter("@DiaChi", dr["DiaChi"].ToString()));
                    param.Add(new SqlParameter("@Tinh", dr["Tinh"].ToString()));
                    param.Add(new SqlParameter("@Huyen", dr["Huyen"].ToString()));
                    param.Add(new SqlParameter("@Xa", dr["Xa"].ToString()));
                    param.Add(new SqlParameter("@DTV", !string.IsNullOrEmpty(dr["DTV"].ToString()) ? dr["DTV"].ToString() : ""));
                    param.Add(new SqlParameter("@KTV", dr["KTV"].ToString()));
                    param.Add(new SqlParameter("@GoiTuVan", dr["GoiTuVan"].ToString()));
                    param.Add(new SqlParameter("@TrangThai", dr["TrangThai"].ToString()));
                    param.Add(new SqlParameter("@KHPhanAnh", dr["KHPhanAnh"].ToString()));
                    param.Add(new SqlParameter("@NgayKy", dr["NgayKy"].ToString()));
                    param.Add(new SqlParameter("@NgayKetThuc", dr["NgayKetThuc"].ToString()));
                    param.Add(new SqlParameter("@SDTLienLac", dr["SDTLienLac"].ToString()));
                    param.Add(new SqlParameter("@SDTKTVSua", dr["SDTKTVSua"].ToString()));
                    param.Add(new SqlParameter("@GhiChu", dr["GhiChu"].ToString()));
                    param.Add(new SqlParameter("@SoHopDong", dr["SoHopDong"].ToString()));

                    if (db3.MyExecuteNonQuery("usp_Service_UpdatePhieuTuVanBaoDuong", CommandType.StoredProcedure, ref err, param))
                        db3.MyExecuteNonQuery("delete PhieuTuVanBaoDuong_Current where SoPhieu like '" + dr["SoPhieu"].ToString() + "'", CommandType.Text, ref err, null);
                }
                catch (Exception ex)
                {
                    logs.ErrorLog(ex.StackTrace, ex.Message);
                }
            }
            else
            {
                logs.ErrorLog("Loi Service ", "Code = " + result);
                soPhieuBIFail++;

                List<SqlParameter> param1 = new List<SqlParameter>();
                param1.Add(new SqlParameter("@iddongbo", idDongBoBI));
                param1.Add(new SqlParameter("@sophieu", dr["SoPhieu"].ToString()));
                param1.Add(new SqlParameter("@trangthai", false));
                db3.MyExecuteNonQuery("usp_Service_ThemLichSuPhieuTVBaoDuong", CommandType.StoredProcedure, ref err, param1);
            }
               
        }

        protected override void OnStart(string[] args)
        {
            logs.ErrorLog("DongBoPhieuService OnStart: " + DateTime.Now, null);


            // Log a service start message to the Application log. 
            // Queue the main service function for execution in a worker thread. 
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadGetData));
        }

        protected override void OnStop()
        {
            logs.ErrorLog("DongBoPhieuService OnStop: " + DateTime.Now, null);
            // Log a service stop message to the Application log. 

            // Indicate that the service is stopping and wait for the finish  
            // of the main service function (ThreadGetData). 
            this.stopping_GetData = true;
            this.stoppedEvent.WaitOne();
        }

        protected override void OnPause()
        {
            logs.ErrorLog("DongBoPhieuService OnPause: " + DateTime.Now, null);

            base.OnPause();
            // timmer.Stop();
            this.stoppedEvent.WaitOne();
        }
        protected override void OnShutdown()
        {
            logs.ErrorLog("DongBoPhieuService OnShutdown: " + DateTime.Now, null);
            base.OnShutdown();
            //timmer.Stop();
            this.stoppedEvent.WaitOne();
        }
    }
}
