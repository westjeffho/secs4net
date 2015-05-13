using System;
using System.Drawing;
using System.Windows.Forms;
using Secs4Net;

namespace SecsDevice {
    public partial class FormLog : Form {
        class SecsLogger : SecsTracer {
            readonly FormLog _form;
            internal SecsLogger(FormLog form) {
                _form = form;
            }
            public override void TraceMessageIn(SecsMessage msg, int systembyte) {
                _form.Invoke((MethodInvoker)delegate {
                    _form.richTextBox1.SelectionColor = Color.Black;
                    _form.richTextBox1.AppendText($"<-- [0x{systembyte:X8}] {msg.ToSML()}\n");
                });
            }

            public override void TraceMessageOut(SecsMessage msg, int systembyte) {
                _form.Invoke((MethodInvoker)delegate {
                    _form.richTextBox1.SelectionColor = Color.Black;
                    _form.richTextBox1.AppendText($"--> [0x{systembyte:X8}] {msg.ToSML()}\n");
                });
            }

            public override void TraceInfo(string msg) {
                _form.Invoke((MethodInvoker)delegate {
                    _form.richTextBox1.SelectionColor = Color.Blue;
                    _form.richTextBox1.AppendText(msg + Environment.NewLine);
                });
            }

            public override void TraceWarning(string msg) {
                _form.Invoke((MethodInvoker)delegate {
                    _form.richTextBox1.SelectionColor = Color.Green;
                    _form.richTextBox1.AppendText(msg + Environment.NewLine);
                });
            }

            public override void TraceError(string msg) {
                _form.Invoke((MethodInvoker)delegate {
                    _form.richTextBox1.SelectionColor = Color.Red;
                    _form.richTextBox1.AppendText(msg + Environment.NewLine);
                });
            }

        }

        public readonly SecsTracer Logger;

        public FormLog() {
            InitializeComponent();

            Logger = new SecsLogger(this);
        }

    }
}
