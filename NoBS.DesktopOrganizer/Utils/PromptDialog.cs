public static class PromptDialog
{
    public static string Show(string text, string defaultValue = "")
    {
        Form prompt = new Form()
        {
            Width = 400,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = text,
            StartPosition = FormStartPosition.CenterScreen
        };
        TextBox inputBox = new TextBox() { Left = 20, Top = 40, Width = 340, Text = defaultValue };
        Button okButton = new Button() { Text = "OK", Left = 280, Width = 80, Top = 70, DialogResult = DialogResult.OK };
        prompt.Controls.Add(inputBox);
        prompt.Controls.Add(okButton);
        prompt.AcceptButton = okButton;

        return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : "";
    }
}
