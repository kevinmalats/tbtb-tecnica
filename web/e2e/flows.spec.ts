import { expect, test } from '@playwright/test';

const actor = '11111111-1111-4111-8111-111111111111';
const apiUrl = process.env['API_URL'] ?? 'http://127.0.0.1:8080';

test('CA01/CA03 agenda and contact history are navigable', async ({ page, request }) => {
  const headers = { 'X-Demo-Actor-Id': actor };
  const suffix = Date.now();
  const patientName = `Paciente E2E ${suffix}`;
  const patientResponse = await request.post(`${apiUrl}/api/v1/patients`, {
    headers,
    data: {
      fullName: patientName, documentCountryCode: 'CO', documentTypeCode: 'PASSPORT',
      documentNumber: `E2E-${suffix}`, phone: '+57 300 000 0000', email: null,
      cityId: 1, treatmentStartDate: new Date().toISOString().slice(0, 10)
    }
  });
  expect(patientResponse.status()).toBe(201);
  const patient = await patientResponse.json();
  const contactResponse = await request.post(`${apiUrl}/api/v1/contacts`, {
    headers,
    data: { patientId: patient.id, occurredAt: new Date(Date.now() - 60_000).toISOString(), channel: 'LLAMADA', result: 'SIN_RESPUESTA' }
  });
  expect(contactResponse.status()).toBe(201);
  const contact = await contactResponse.json();

  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Resumen operativo' })).toBeVisible();
  await page.getByRole('button', { name: 'Pacientes' }).click();
  await page.getByRole('button', { name: patientName, exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Detalle del paciente' })).toBeVisible();
  await expect(page).toHaveURL(new RegExp(`/patients/${patient.id}$`));
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Detalle del paciente' })).toBeVisible();
  await page.getByRole('button', { name: 'Cerrar' }).click();
  await page.getByRole('button', { name: 'Agenda' }).click();
  await expect(page.getByRole('heading', { name: 'Agendar seguimiento' })).toBeVisible();
  await expect(page.getByLabel('Paciente')).toContainText(patientName);

  await page.getByRole('button', { name: 'Contactos' }).click();
  await expect(page.getByRole('heading', { name: 'Contactos del mes' })).toBeVisible();
  await page.getByRole('button', { name: new RegExp(patientName) }).click();
  await expect(page.getByRole('heading', { name: 'Detalle del contacto' })).toBeVisible();
  await expect(page.getByText('Revisión 1 · SIN RESPUESTA')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Registrar corrección' })).toBeVisible();

  const competingCorrection = await request.post(`${apiUrl}/api/v1/contacts/${contact.id}/corrections`, {
    headers,
    data: {
      occurredAt: contact.occurredAt, channel: contact.channel, result: 'CONTACTADO',
      reason: 'Corrección guardada desde una segunda ventana E2E.', expectedVersion: contact.version
    }
  });
  expect(competingCorrection.status()).toBe(200);

  const dialog = page.locator('section').filter({ has: page.getByRole('heading', { name: 'Detalle del contacto' }) });
  await dialog.getByLabel('Resultado').selectOption('FALLIDO');
  await dialog.getByLabel('Motivo').fill('Intento deliberado con una versión anterior del contacto.');
  await dialog.getByRole('button', { name: 'Guardar corrección' }).click();
  await expect(page.getByRole('button', { name: 'Recargar contacto' })).toBeVisible();
  await expect(dialog.getByLabel('Motivo')).toHaveValue('Intento deliberado con una versión anterior del contacto.');
});
