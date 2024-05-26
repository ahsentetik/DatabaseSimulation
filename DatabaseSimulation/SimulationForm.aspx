<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SimulationForm.aspx.cs" Inherits="DatabaseSimulation.SimulationForm" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>Simulation Form</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
        }

        .container {
            max-width: 600px;
            margin: 50px auto;
            background-color: #fff;
            padding: 20px;
            border-radius: 5px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        }

        .form-group {
            margin-bottom: 20px;
        }

        .form-group label {
            display: block;
            margin-bottom: 5px;
            font-size: 16px;
        }

        .form-group input[type="text"],
        .form-group select {
            width: 100%;
            padding: 12px;
            font-size: 16px;
            border: 1px solid #ccc;
            border-radius: 4px;
            box-sizing: border-box;
        }

        .form-group button {
            padding: 12px 24px;
            font-size: 16px;
            background-color: #007bff;
            color: #fff;
            border: none;
            border-radius: 4px;
            cursor: pointer;
        }

        .form-group button:hover {
            background-color: #0056b3;
        }

        .result {
            margin-top: 20px;
            padding: 10px;
            border: 1px solid #ccc;
            border-radius: 4px;
            background-color: #f9f9f9;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }

        th, td {
            padding: 10px;
            border: 1px solid #ddd;
            vertical-align: middle;
        }

        th {
            background-color: #f2f2f2;
            font-weight: bold;
            text-align: center;
        }

        td {
            text-align: right;
            height: 50px;
        }

        td, th {
            font-size: 14px;
        }
    </style>
</head>
<body>
    <div class="container">
        <form id="form1" runat="server">
            <div class="form-group">
                <label for="txtTypeA">Number of Type A Users:</label>
                <asp:TextBox ID="txtTypeA" runat="server"></asp:TextBox>
            </div>
            <div class="form-group">
                <label for="txtTypeB">Number of Type B Users:</label>
                <asp:TextBox ID="txtTypeB" runat="server"></asp:TextBox>
            </div>
            <div class="form-group">
                <label for="ddlIsolationLevel">Isolation Level:</label>
                <asp:DropDownList ID="ddlIsolationLevel" runat="server">
                    <asp:ListItem Text="Read Uncommitted" Value="ReadUncommitted"></asp:ListItem>
                    <asp:ListItem Text="Read Committed" Value="ReadCommitted"></asp:ListItem>
                    <asp:ListItem Text="Repeatable Read" Value="RepeatableRead"></asp:ListItem>
                    <asp:ListItem Text="Serializable" Value="Serializable"></asp:ListItem>
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Button ID="btnSimulate" runat="server" Text="Start Simulation" OnClick="btnSimulate_Click" />
            </div>
            <div class="form-group result">
                <asp:Label ID="lblResult" runat="server" Text=""></asp:Label>
            </div>
        </form>
        <div class="form-group result">
            <asp:Literal ID="litReport" runat="server"></asp:Literal>
        </div>
    </div>
</body>
</html>
