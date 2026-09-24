import { describe, expect, it, vi } from 'vitest';
import { WorkspacePort } from '../application/workspace.port';
import { WorkspaceFacade } from './workspace.facade';

describe('WorkspaceFacade', () => {
  it('delegates actor selection and patient loading through the application port', async () => {
    const patients = { items: [], total: 0, page: 1, pageSize: 20 };
    const port = {
      setActor: vi.fn(),
      getPatients: vi.fn().mockResolvedValue(patients)
    } as unknown as WorkspacePort;
    const facade = new WorkspaceFacade(port);

    facade.setActor('actor-a');
    const result = await facade.getPatients();

    expect(port.setActor).toHaveBeenCalledWith('actor-a');
    expect(port.getPatients).toHaveBeenCalledOnce();
    expect(result).toBe(patients);
  });

  it('delegates patient creation without transport knowledge', async () => {
    const command = { fullName: 'Paciente', documentCountryCode: 'CO', documentTypeCode: 'NATIONAL_ID', documentNumber: '123', phone: '3000000000', email: null, cityId: 1, treatmentStartDate: '2026-09-24' };
    const patient = { id: 'patient-a', ...command, cityName: 'Bogota', assignedManagerId: 'actor-a' };
    const port = { createPatient: vi.fn().mockResolvedValue(patient) } as unknown as WorkspacePort;
    const facade = new WorkspaceFacade(port);

    await expect(facade.createPatient(command)).resolves.toBe(patient);
    expect(port.createPatient).toHaveBeenCalledWith(command);
  });
});
