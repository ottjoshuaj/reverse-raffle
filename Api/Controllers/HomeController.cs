using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;

namespace ReverseRaffler.Api.Controllers
{
    public class HomeController : ApiController
    {
        /// <summary>
        /// Serves the mobile-friendly HTML page at the root
        /// </summary>
        [HttpGet]
        [Route("")]
        public HttpResponseMessage Get()
        {
            var html = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reverse Raffle</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container {
            background: white;
            padding: 40px 30px;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 400px;
            width: 100%;
        }
        h1 {
            text-align: center;
            color: #333;
            margin-bottom: 10px;
            font-size: 28px;
        }
        .subtitle {
            text-align: center;
            color: #666;
            margin-bottom: 30px;
            font-size: 14px;
        }
        .form-group {
            margin-bottom: 20px;
        }
        label {
            display: block;
            margin-bottom: 8px;
            color: #555;
            font-weight: 600;
            font-size: 14px;
        }
        .input-row {
            display: flex;
            gap: 10px;
        }
        input[type='number'] {
            flex: 1;
            padding: 14px;
            border: 2px solid #e0e0e0;
            border-radius: 10px;
            font-size: 18px;
            transition: border-color 0.3s;
            -moz-appearance: textfield;
        }
        input[type='number']::-webkit-outer-spin-button,
        input[type='number']::-webkit-inner-spin-button {
            -webkit-appearance: none;
            margin: 0;
        }
        input[type='number']:focus {
            outline: none;
            border-color: #667eea;
        }
        button {
            padding: 14px 24px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border: none;
            border-radius: 10px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: transform 0.2s, box-shadow 0.2s;
        }
        button:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 20px rgba(102, 126, 234, 0.4);
        }
        button:active {
            transform: translateY(0);
        }
        .message {
            margin-top: 20px;
            padding: 12px;
            border-radius: 8px;
            text-align: center;
            font-weight: 500;
            display: none;
        }
        .message.success {
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }
        .message.error {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }
        @media (max-width: 480px) {
            .container {
                padding: 30px 20px;
            }
            h1 {
                font-size: 24px;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🎟️ Reverse Raffle</h1>
        <p class='subtitle'>Enter your ticket number</p>

        <form id='ticketForm'>
            <div class='form-group'>
                <label for='ticketNumber'>Ticket Number</label>
                <div class='input-row'>
                    <input type='number' id='ticketNumber' name='ticketNumber'
                           placeholder='Enter number' required autofocus>
                    <button type='submit'>Submit</button>
                </div>
            </div>
        </form>

        <div id='message' class='message'></div>
    </div>

    <script>
        const form = document.getElementById('ticketForm');
        const input = document.getElementById('ticketNumber');
        const messageDiv = document.getElementById('message');

        form.addEventListener('submit', async (e) => {
            e.preventDefault();

            const ticketNumber = input.value;

            if (!ticketNumber) {
                showMessage('Please enter a ticket number', 'error');
                return;
            }

            try {
                const response = await fetch(`/api/ticket?number=${ticketNumber}`, {
                    method: 'GET'
                });

                if (response.ok) {
                    showMessage(`Ticket #${ticketNumber} submitted successfully!`, 'success');
                    input.value = '';
                    input.focus();
                } else {
                    showMessage('Failed to submit ticket. Please try again.', 'error');
                }
            } catch (error) {
                showMessage('Connection error. Please check your connection.', 'error');
            }
        });

        function showMessage(text, type) {
            messageDiv.textContent = text;
            messageDiv.className = 'message ' + type;
            messageDiv.style.display = 'block';

            setTimeout(() => {
                messageDiv.style.display = 'none';
            }, 3000);
        }
    </script>
</body>
</html>";

            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(html, Encoding.UTF8, "text/html");
            return response;
        }
    }
}
