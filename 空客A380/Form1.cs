using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 空客A380
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            T0.Elapsed += new System.Timers.ElapsedEventHandler(T0_Fun);//指定到时执行函数
            T0.Enabled = true;//启动
            M_Devtype = 4;//USBCAN2型
            M_Connect = false;//未连接
            fileName = textBox2.Text + ".txt";
        }
       
        //CAN相关
        #region 定义变量
        public bool M_Connect;//连接设备标志位
        public uint M_Devtype;//设备类型
        public uint M_Devind;//设备索引号
        public uint M_Chaind;//通道号
        public int Channel_Flag = 0;//通道号标志位
        public bool Channel0_Start = false;//通道0启动标志位
        public bool Channel1_Start = false;//通道1启动标志位
        public bool Channel_Start = false;//通道启动标志位
        public int Frame_Num0 = 0;//通道0数据频率
        public string Path = "";//记录路径
        public bool Collect_Flag = false;//采集标志位
        public string Path_Get = "";//获取的文件选择框的路径 
        System.Timers.Timer T0 = new System.Timers.Timer(100);//创建定时器T0，并设置时间间隔为10ms，处理CAN数据
        string fileName = string.Empty;
        
        #endregion

        #region 定义CAN相关结构体
        //1.ZLGCAN系列接口卡信息的数据类型。
        public struct VCI_BOARD_INFO
        {
            public UInt16 hw_Version;
            public UInt16 fw_Version;
            public UInt16 dr_Version;
            public UInt16 in_Version;
            public UInt16 irq_Num;
            public byte can_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] str_Serial_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public byte[] str_hw_Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Reserved;
        }
        //2.定义CAN信息帧的数据类型。
        unsafe public struct VCI_CAN_OBJ
        {
            public uint ID;
            public uint TimeStamp;
            public byte TimeFlag;
            public byte SendType;
            public byte RemoteFlag;//是否是远程帧
            public byte ExternFlag;//是否是扩展帧
            public byte DataLen;
            public fixed byte Data[8];

            public fixed byte Reserved[3];

        }
        //3.定义CAN控制器状态的数据类型。
        public struct VCI_CAN_STATUS
        {
            public byte ErrInterrupt;
            public byte regMode;
            public byte regStatus;
            public byte regALCapture;
            public byte regECCapture;
            public byte regEWLimit;
            public byte regRECounter;
            public byte regTECounter;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Reserved;
        }
        //4.定义错误信息的数据类型。
        struct VCI_ERR_INFO
        {
            public UInt32 ErrCode;
            public byte Passive_ErrData1;
            public byte Passive_ErrData2;
            public byte Passive_ErrData3;
            public byte ArLost_ErrData;
        }
        //5.定义初始化CAN的数据类型
        public struct VCI_INIT_CONFIG
        {
            public UInt32 AccCode;
            public UInt32 AccMask;
            public UInt32 Reserved;
            public byte Filter;
            public byte Timing0;
            public byte Timing1;
            public byte Mode;
        }
        #endregion
        #region //--------引用CAN控制函数库---------------//

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_OpenDevice(UInt32 DeviceType, UInt32 DeviceInd, UInt32 Reserved);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_CloseDevice(UInt32 DeviceType, UInt32 DeviceInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_InitCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_INIT_CONFIG pInitConfig);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ReadBoardInfo(UInt32 DeviceType, UInt32 DeviceInd, ref VCI_BOARD_INFO pInfo);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ReadErrInfo(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_ERR_INFO pErrInfo);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ReadCANStatus(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_STATUS pCANStatus);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_GetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, ref byte pData);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_SetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, ref byte pData);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_GetReceiveNum(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ClearBuffer(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_StartCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ResetCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_Transmit(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_OBJ pSend, UInt32 Len);

        [DllImport("controlcan.dll", CharSet = CharSet.Ansi)]
        static extern UInt32 VCI_Receive(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, IntPtr pReceive, UInt32 Len, Int32 WaitTime);

        static UInt32 m_devtype = 4;//USBCAN2

        UInt32 m_bOpen = 0;
        UInt32 m_devind = 0;
        UInt32 m_canind = 0;

        VCI_CAN_OBJ[] m_recobj = new VCI_CAN_OBJ[50];

        UInt32[] m_arrdevtype = new UInt32[20];
        #endregion
        //CAN数据处理函数
        unsafe public void T0_Fun(object source, System.Timers.ElapsedEventArgs e)
        {
            UInt32 res = new UInt32();
            res = VCI_GetReceiveNum(M_Devtype, M_Devind, 0);//通道0接收的数据个数
            if (res == 0)
                return;
            /////////////////////////////////////
            UInt32 con_maxlen = 50;//定义接收最大帧数
            IntPtr pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)) * (Int32)con_maxlen);//申请大小为  “VCI_CAN_OBJ”类型占用内存乘上最大帧数  的内存
            res = VCI_Receive(M_Devtype, M_Devind, m_canind, pt, con_maxlen, 100);//通道0接收的数据
            ////////////////////////////////////////////////////////
            String str = "";
            for (UInt32 i = 0; i < res; i++)
            {
                VCI_CAN_OBJ obj = (VCI_CAN_OBJ)Marshal.PtrToStructure((IntPtr)((UInt32)pt + i * Marshal.SizeOf(typeof(VCI_CAN_OBJ))), typeof(VCI_CAN_OBJ));//指针地址每次增加一个“VCI_CAN_OBJ”类型占用内存数

                str = "接收到数据: ";
                str += "  帧ID:0x" + System.Convert.ToString((Int32)obj.ID, 16);//获取帧ID
                str += "  帧格式:";
                if (obj.RemoteFlag == 0)
                    str += "数据帧 ";
                else
                    str += "远程帧 ";
                if (obj.ExternFlag == 0)
                    str += "标准帧 ";
                else
                    str += "扩展帧 ";

                //////////////////////////////////////////
                if (obj.RemoteFlag == 0)//此帧为数据帧
                {
                    str += "数据: ";
                    string Str0 = "";

                    byte[] T0_Data = new byte[8];//存储数据

                    for (int j = 0; j <= 7; j++)//循环接收8个字节
                    {
                        string Str_Tem;
                        Str_Tem = "0" + System.Convert.ToString(obj.Data[j], 16).ToUpper();//转换为16进制大写字符
                        Str_Tem = Str_Tem.Substring(Str_Tem.Length - 2, 2);//取右边2位
                        Str0 += Str_Tem + " ";//字符串叠加
                        T0_Data[j] = obj.Data[j];//赋值8个字节的数据
                    }
                    str = str + Str0+"\n";//完整的字符串信息
                    
                    double X_Angle = 0, Y_Angle = 0;
                    long intXAngle, intYAngle;
                    //------------处理X轴数据-------------//
                    intXAngle = (T0_Data[3] * 256 * 256 * 256 + T0_Data[2] * 256 * 256 + T0_Data[1] * 256 + T0_Data[0] );
                    intYAngle = (T0_Data[7] * 256 * 256 * 256 + T0_Data[6] * 256 * 256 + T0_Data[5] * 256 + T0_Data[4]);

                    //if (T0_Data[3]==0xff)//负数
                    //    intXAngle = (intXAngle - 4294967296) ;
                    //if (T0_Data[7] == 0xff)//负数
                    //    intYAngle = (intYAngle - 4294967296) ;

                    X_Angle = intXAngle *0.001;
                    Y_Angle = intYAngle *0.001;
                    Invoke(new MethodInvoker(delegate () { textBox_P1X.Text = X_Angle.ToString("0.000"); }));
                    Invoke(new MethodInvoker(delegate () { textBox_P1Y.Text = Y_Angle.ToString("0.000"); }));
                    Invoke(new MethodInvoker(delegate () { textBox3.AppendText(str); }));


                    //------------------采集数据-------------------//
                    if (Collect_Flag == true)
                    {
                        if (!File.Exists(Path))//文件不存在
                        {
                            FileStream File_WR = new FileStream(Path, FileMode.Create,  FileAccess.Write);//创建写入文件  
                             File_WR.Close();
                        }
                                //---------------记录数据--------------//
                        //try
                        //{
                            using (FileStream File_W = new FileStream(Path, FileMode.Append, FileAccess.Write))//附加方式写入
                            {
                                StreamWriter sr = new StreamWriter(File_W);
                                sr.WriteLine(DateTime.Now + "  "  + "  " + X_Angle.ToString("0.000") + "   " + Y_Angle.ToString("0.000"));//写入值
                                sr.Close();
                                File_W.Close();
                            }
                        //}
                        //catch
                        //{

                        //}
                    }
                }
            }
            Marshal.FreeHGlobal(pt);//释放内存
        }

        private void button_Connect_Click(object sender, EventArgs e)
        {
            uint FunRec;
            if (M_Connect == true)//当前已连接设备
            {
                VCI_CloseDevice(M_Devtype, M_Devind);//关闭设备连接
                //Delay(1000);//延时1s
                M_Connect = false;//清零标志位
                button_Connect.Text = "连接";
            }
            else//当前未连接
            {
                if (comboBox_DevIndex.SelectedIndex != -1 && comboBox_ChaIndex.SelectedIndex != -1)//设备索引号和通道号都不为空
                {
                    FunRec = VCI_OpenDevice(M_Devtype, M_Devind, 0);//设备连接
                    if (FunRec != 1)
                    {
                        MessageBox.Show("打开设备错误", "提示");
                    }
                    else
                    {
                        M_Connect = true;//清零标志位
                        button_Connect.Text = "断开";
                    }
                }

            }
        }

        private void comboBox_ChaIndex_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_ChaIndex.SelectedIndex == 0)//当前为通道0
                Channel_Start = Channel0_Start;
            else                                     //当前为通道1
                Channel_Start = Channel1_Start;

            if (Channel_Start == true)
                button_Start.Text = "停止";
            else
                button_Start.Text = "启动";
        }

        private void comboBox_BaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            int Len = 0;
            int Baud = 0;
            Len = comboBox_BaudRate.Text.Length;//获取字符数
            if (Len == 7)
                Baud = Convert.ToInt16(comboBox_BaudRate.Text.Substring(0, 3));
            else if (Len == 8)
                Baud = Convert.ToInt16(comboBox_BaudRate.Text.Substring(0, 4));
            switch (Baud)
            {
                case 125:
                    textBox_Timer0.Text = "03";
                    textBox_Timer1.Text = "1C";
                    break;
                case 250:
                    textBox_Timer0.Text = "01";
                    textBox_Timer1.Text = "1C";
                    break;
                case 500:
                    textBox_Timer0.Text = "00";
                    textBox_Timer1.Text = "1C";
                    break;
                case 800:
                    textBox_Timer0.Text = "00";
                    textBox_Timer1.Text = "16";
                    break;
                case 1000:
                    textBox_Timer0.Text = "00";
                    textBox_Timer1.Text = "14";
                    break;
            }
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            uint FunRec;
            VCI_INIT_CONFIG InitConfig = new VCI_INIT_CONFIG { };//定义一个初始化参数结构体
            M_Devind = Convert.ToUInt32(comboBox_DevIndex.Text);//获取设备索引号
            M_Chaind = Convert.ToUInt32(comboBox_ChaIndex.Text);//获取通道号

            if (M_Connect == false)
            {
                MessageBox.Show("请先连接设备", "提示");
                return;
            }
            //------------读取标志位-----------------//
            if (comboBox_ChaIndex.SelectedIndex == 0)//当前为通道0
                Channel_Start = Channel0_Start;
            else                                     //当前为通道1
                Channel_Start = Channel1_Start;
            //------------------------------------------
            if (Channel_Start == true)//通道已启动
            {
                VCI_ResetCAN(M_Devtype, M_Devind, M_Chaind);//复位
                button_Start.Text = "启动";
                Channel_Start = false;
            }
            else                    //通道未启动
            {
                //-----赋值相关参数------//
                InitConfig.AccCode = Convert.ToUInt32("0x" + textBox_Code.Text, 16);//验收码
                InitConfig.AccMask = Convert.ToUInt32("0x" + textBox_Mask.Text, 16);//屏蔽码
                InitConfig.Filter = (Byte)(comboBox_Filter.SelectedIndex);//滤波方式
                InitConfig.Mode = (Byte)(comboBox_Mode.SelectedIndex);//模式
                InitConfig.Timing0 = Convert.ToByte("0x" + textBox_Timer0.Text, 16);//定时器0
                InitConfig.Timing1 = Convert.ToByte("0x" + textBox_Timer1.Text, 16);//定时器1
                //-----初始化CAN------//
                FunRec = VCI_InitCAN(M_Devtype, M_Devind, M_Chaind, ref InitConfig);
                if (FunRec != 1)//初始化失败
                {
                    MessageBox.Show("初始化CAN错误", "提示");
                    return;
                }
                else//初始化成功
                {
                    FunRec = VCI_StartCAN(M_Devtype, M_Devind, M_Chaind);//启动通道
                    if (FunRec != 1)
                    {
                        MessageBox.Show("启动CAN错误", "提示");
                        return;
                    }
                    else
                    {
                        button_Start.Text = "停止";
                        Channel_Start = true;
                    }
                }
            }
            //------------写入标志位-----------------//
            if (comboBox_ChaIndex.SelectedIndex == 0)//当前为通道0
                Channel0_Start = Channel_Start;
            else                                     //当前为通道1
                Channel1_Start = Channel_Start;
        }

        private void button_Reset_Click(object sender, EventArgs e)
        {
            if (M_Connect == true)
            {
                VCI_ResetCAN(M_Devtype, M_Devind, 0);//复位通道0
                VCI_ResetCAN(M_Devtype, M_Devind, 1);//复位通道1
            }
        }
        private void button1_Click_1(object sender, EventArgs e)
        {

            if (Path != "")//不是第一次打开，使用上次的目录
            {
                this.folderBrowserDialog1.SelectedPath = Path_Get;
            }
            //this.folderBrowserDialog1.ShowDialog();//打开文件浏览
            //Path_Get = this.folderBrowserDialog1.SelectedPath;//获取路径
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)//选择了一个路径
            {
                Path_Get = this.folderBrowserDialog1.SelectedPath;//获取路径
                Path = Path_Get;//更新路径
                textBox1.Text = Path;
            }
            else
            {
                return;
            }

            Path = Path  + fileName;//增加文件名称
            if (!File.Exists(Path))//文件不存在
            {
                FileStream File_WR = new FileStream(Path, FileMode.Create, FileAccess.ReadWrite);//创建文件 
                File_WR.Close();
            }
            //Collect_Flag = true;//置位标志位
            //button_DataCollect.Text = "停止采集";

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == "采集")
            {
                Collect_Flag = true;
                button2.Text = "停止采集";
                textBox2.Enabled = false;
            }
            else
            {
                Collect_Flag = false;
                button2.Text = "采集";
                textBox2.Enabled = true;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            fileName = "\\" + textBox2.Text + ".txt";
            Path += fileName;
        }
    }
}
