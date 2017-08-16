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
        private int typeRun = 0;
        private static string strConnect1 = ConfigurationManager.AppSettings["ConnectASC"].ToString();
        private static string strConnect2 = ConfigurationManager.AppSettings["Connect31"].ToString();
        private static string strConnect3 = ConfigurationManager.AppSettings["Connect"].ToString();
        bool stopping_GetData = false;
        ASCService.asc_serviceSoapClient client2 = new ASCService.asc_serviceSoapClient();
        private static DataAccess db2 = new DataAccess(strConnect2);
        private static DataAccess db1 = new DataAccess(strConnect1);
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

                try
                {

                    DataTable dt01 = db3.ExecuteQueryDataSet("select * from PhieuSuaChua_Current  where iFlag = 0", CommandType.Text, null);
                    DataTable dt02 = db3.ExecuteQueryDataSet("select * from PhieuVOC_IH_Curent where iFlag = 0", CommandType.Text, null);
                    DataTable dt03 = db3.ExecuteQueryDataSet("select * from PhieuTuVanBaoDuong_Current where iFlag = 0", CommandType.Text, null);

                    if (dt01.Rows.Count > 0)
                    {

                        foreach (DataRow dr in dt01.Rows)
                        {
                            UpdatePhieuBH(dr);
                            // UpdatePhieuVOCIH(dr);
                        }
                    }

                    if (dt02.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt02.Rows)
                        {
                            //if (!UpdatePhieuBH(dr))
                            //    MessageBox.Show("false");
                            UpdatePhieuVOCIH(dr);
                        }
                    }

                    if (dt03.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt03.Rows)
                        {
                            UpdatePhieuBI(dr);
                        }
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

            try
            {

                //  result = client2.UpdatePhieu("asc.portal", "abc@123", 1, "IH117081500046", "",
                //"15/08/2017", "10:22:32", 2, "15/08/2017", "10:22:32",
                //"1107", "Nguyễn thị trúc", "100", "", "Hoàng Xá", "717", "0710", "071001", "BLCT01", "Mô tả lỗi", "0000047", 0, "", "Ghi chú", 98);

                result = client2.UpdatePhieu(ten_dn, mat_khau, kieunhap, sophieu, makho,
                ngaytiepnhan, giotiepnhan, loaitiepnhan, ngayhentra, giohentra,
                nguoitn, tenkh, sdt, def, diachi, tinh, huyen, xa, maht, motaloi, serial, kieuphieu, goibaoduong, ghichu, trangthai);
            }
            catch (Exception ex)
            {
                return false;
            }


            if (result == 200 || result == 201)
            {
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

                    if (db3.MyExecuteNonQuery("usp_Service_ThemPhieuBaoHanh", CommandType.StoredProcedure, ref err, param))
                    {
                        db3.MyExecuteNonQuery("delete PhieuSuaChua_Current where SoPhieu like '" + dr["SoPhieu"].ToString() + "'", CommandType.Text, ref err, null);
                    }


                }
                catch (Exception ex)
                {
                    return false;
                    //lg.Error(ex);
                }
            }
            return true;
        }

        public void UpdatePhieuVOCIH(DataRow dr)
        {
            int result = 0;
            int phieuchinh = 0;
            string err = "";
            string username = "";
            try
            {
                username = db3.MyExecuteScalar("select PhanCongKTV from PhieuSuaChua_ASC where SoPhieu like '" + dr["SoPhieuIH"].ToString() + "'", CommandType.Text, ref err, null).ToString();
            }
            catch (Exception ex)
            {

            }

            try
            {
                phieuchinh = (bool.Parse(dr["PhieuChinh"].ToString()) == true) ? 1 : 0;
                result = client2.UpdateVOC("asc.portal", "abc@123", 1, dr["SoPhieu"].ToString(), dr["SoPhieuIH"].ToString(),
                username, DateTime.Parse(dr["NgayGio"].ToString()).ToString("dd/MM/yyyy"),
                DateTime.Parse(dr["NgayGio"].ToString()).ToString("HH:mm:ss"), dr["KHPhanAnh"].ToString(),
                phieuchinh, int.Parse(dr["MucDo"].ToString()), int.Parse(dr["TrangThai"].ToString()));
            }
            catch (Exception ex)
            {

            }

            if (result == 200 || result == 201)
            {
                List<SqlParameter> param = new List<SqlParameter>();

                param.Add(new SqlParameter("@SoPhieu", dr["SoPhieu"].ToString()));
                param.Add(new SqlParameter("@SoPhieuIH", dr["SoPhieuIH"].ToString()));
                param.Add(new SqlParameter("@KHPhanAnh", dr["KHPhanAnh"].ToString()));
                param.Add(new SqlParameter("@GhiChu", dr["GhiChu"].ToString()));
                param.Add(new SqlParameter("@TrangThai", dr["TrangThai"].ToString()));
                param.Add(new SqlParameter("@PhieuChinh", bool.Parse(dr["PhieuChinh"].ToString())));
                param.Add(new SqlParameter("@MucDo", int.Parse(dr["MucDo"].ToString())));
                param.Add(new SqlParameter("@NgayGio", dr["NgayGio"].ToString()));

                if (db3.MyExecuteNonQuery("usp_Service_InsertOrUpdatePhieuVOC_IH", CommandType.StoredProcedure, ref err, param))
                    db3.MyExecuteNonQuery("delete PhieuVOC_IH_Curent where SoPhieu like '" + dr["SoPhieu"].ToString() + "'", CommandType.Text, ref err, null);
            }
        }

        public void UpdatePhieuBI(DataRow dr)
        {
            int result = 0;
            string err = "";
            try
            {
                result = client2.UpdatePhieu("asc.portal", "abc@123", 1, dr["SoPhieu"].ToString(), "",
                    DateTime.Parse(dr["NgayTiepNhan"].ToString()).ToString("dd/MM/yyyy"),
                    DateTime.Parse(dr["NgayTiepNhan"].ToString()).ToString("HH:mm:ss"),
                     0, "", "", dr["NguoiTiepNhan"].ToString(), dr["TenKH"].ToString(), dr["SDT"].ToString(), "",
                     dr["DiaChi"].ToString(), dr["Tinh"].ToString(), dr["Huyen"].ToString(), dr["Xa"].ToString(), "", "", "", 2,
                     dr["GoiTuVan"].ToString(), dr["KHPhanAnh"].ToString(), int.Parse(dr["TrangThai"].ToString()));
            }
            catch (Exception ex) { }

            if (result == 200 || result == 201)
                try
                {
                    List<SqlParameter> param = new List<SqlParameter>();
                    param.Add(new SqlParameter("@SoPhieu", dr["SoPhieu"].ToString()));
                    param.Add(new SqlParameter("@NgayTiepNhan", dr["NgayTiepNhan"].ToString()));
                    param.Add(new SqlParameter("@NguoiTiepNhan", int.Parse(dr["NguoiTiepNhan"].ToString())));
                    param.Add(new SqlParameter("@DonVi", dr["DonVi"].ToString()));
                    param.Add(new SqlParameter("@SDT", dr["SDT"].ToString()));
                    param.Add(new SqlParameter("@TenKH", dr["TenKH"].ToString()));
                    param.Add(new SqlParameter("@DiaChi", dr["DiaChi"].ToString()));
                    param.Add(new SqlParameter("@Tinh", dr["Tinh"].ToString()));
                    param.Add(new SqlParameter("@Huyen", dr["Huyen"].ToString()));
                    param.Add(new SqlParameter("@Xa", dr["Xa"].ToString()));
                    param.Add(new SqlParameter("@DTV", int.Parse(dr["DTV"].ToString())));
                    param.Add(new SqlParameter("@KTV", dr["KTV"].ToString()));
                    param.Add(new SqlParameter("@GoiTuVan", dr["GoiTuVan"].ToString()));
                    param.Add(new SqlParameter("@TrangThai", dr["TrangThai"].ToString()));
                    param.Add(new SqlParameter("@KHPhanAnh", dr["KHPhanAnh"].ToString()));
                    param.Add(new SqlParameter("@NgayKy", dr["NgayKy"].ToString()));
                    param.Add(new SqlParameter("@NgayKetThuc", dr["NgayKetThuc"].ToString()));

                    if (db3.MyExecuteNonQuery("usp_Service_UpdatePhieuTuVanBaoDuong", CommandType.StoredProcedure, ref err, param))
                        db3.MyExecuteNonQuery("delete PhieuTuVanBaoDuong_Current where SoPhieu like '" + dr["SoPhieu"].ToString() + "'", CommandType.Text, ref err, null);
                }
                catch (Exception ex)
                {

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
